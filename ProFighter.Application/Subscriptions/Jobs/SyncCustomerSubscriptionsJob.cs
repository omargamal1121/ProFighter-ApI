using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Exceptions;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Jobs;

public class SyncCustomerSubscriptionsJob
{
    private readonly IRekazClientFactory _clientFactory;
    private readonly IApplicationDbContext _context;
    private readonly IRekazCustomerSyncService _customerSyncService;
    private readonly ISubscriptionUpsertService _subscriptionUpsertService;
    private readonly ILogger<SyncCustomerSubscriptionsJob> _logger;

    public SyncCustomerSubscriptionsJob(
        IRekazClientFactory clientFactory,
        IApplicationDbContext context,
        IRekazCustomerSyncService customerSyncService,
        ISubscriptionUpsertService subscriptionUpsertService,
        ILogger<SyncCustomerSubscriptionsJob> logger)
    {
        _clientFactory = clientFactory;
        _context = context;
        _customerSyncService = customerSyncService;
        _subscriptionUpsertService = subscriptionUpsertService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 5, 30, 120 }, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(
        GymType gymType,
        Guid rekazCustomerId,
        DateTimeOffset transactionCreatedAtUtc,
        List<Guid> productIds,
        PerformContext? performContext = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting SyncCustomerSubscriptionsJob for {GymType}, RekazCustomerId={RekazCustomerId}", gymType, rekazCustomerId);

        try
        {
            // Step b: Ensure local customer exists
            await _customerSyncService.EnsureLocalCustomerAsync(rekazCustomerId, gymType, ct);

            // Step c: Fetch customer subscriptions from Rekaz
            var rekazClient = _clientFactory.GetClient(gymType);
            var subscriptions = await rekazClient.Subscriptions.GetSubscriptionsByCustomerAsync(rekazCustomerId, ct);

            _logger.LogInformation("Fetched {Count} subscriptions for RekazCustomerId={RekazCustomerId} in {GymType}",
                subscriptions.Count, rekazCustomerId, gymType);

            // Step d: Upsert ALL returned subscriptions (Pending included)
            var insertedCount = 0;
            var updatedCount = 0;

            foreach (var sub in subscriptions)
            {
                var result = await _subscriptionUpsertService.UpsertSubscriptionAsync(sub, gymType, ct);
                if (result == SubscriptionUpsertResult.Created)
                    insertedCount++;
                else if (result == SubscriptionUpsertResult.Updated)
                    updatedCount++;
            }

            // Step e: Call SaveChangesAsync BEFORE match evaluation with unique constraint recovery
            await SaveChangesWithRaceRecoveryAsync(gymType, rekazCustomerId, subscriptions, ct);

            // Match evaluation for eventual consistency
            var toleranceMin = transactionCreatedAtUtc.AddMinutes(-2);
            var hasMatchingSubscription = subscriptions.Any(sub =>
            {
                var matchesProduct = sub.ProductId.HasValue && productIds.Contains(sub.ProductId.Value)
                                  || sub.PriceId.HasValue && productIds.Contains(sub.PriceId.Value);

                var timestamp = sub.CreationTime ?? sub.LastModificationTime;
                var matchesTime = timestamp.HasValue && timestamp.Value >= toleranceMin;

                return matchesProduct && matchesTime;
            });

            if (!hasMatchingSubscription)
            {
                var retryCount = performContext?.GetJobParameter<int>("RetryCount") ?? 0;
                var productIdsStr = string.Join(", ", productIds);

                if (retryCount >= 3)
                {
                    _logger.LogWarning(
                        "Final retry attempt ({RetryCount}) exhausted for RekazCustomerId={RekazCustomerId} in {GymType}. " +
                        "No subscription matching product IDs [{ProductIds}] created around {CreatedAt:O} was found in Rekaz. Nightly sync will act as safety net.",
                        retryCount, rekazCustomerId, gymType, productIdsStr, transactionCreatedAtUtc);
                }
                else
                {
                    _logger.LogWarning(
                        "Matching subscription for RekazCustomerId={RekazCustomerId} ({GymType}) with product IDs [{ProductIds}] created at {CreatedAt:O} not yet visible. Attempt {Attempt}.",
                        rekazCustomerId, gymType, productIdsStr, transactionCreatedAtUtc, retryCount + 1);

                    throw new SubscriptionNotYetVisibleException(gymType, rekazCustomerId, transactionCreatedAtUtc, productIds);
                }
            }

            _logger.LogInformation(
                "Completed SyncCustomerSubscriptionsJob for {GymType}, RekazCustomerId={RekazCustomerId}: {InsertedCount} inserted, {UpdatedCount} updated.",
                gymType, rekazCustomerId, insertedCount, updatedCount);
        }
        catch (Exception ex) when (ex is not SubscriptionNotYetVisibleException && ex is not RekazCustomerFilterNotSupportedException)
        {
            _logger.LogError(ex, "SyncCustomerSubscriptionsJob failed for {GymType}, RekazCustomerId={RekazCustomerId}", gymType, rekazCustomerId);
            throw;
        }
    }

    private async Task SaveChangesWithRaceRecoveryAsync(
        GymType gymType,
        Guid rekazCustomerId,
        List<RekazSubscriptionResult> subscriptions,
        CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.LogWarning(ex, "Unique constraint violation during SaveChangesAsync for RekazCustomerId={RekazCustomerId} in {GymType}. Clearing ChangeTracker and re-running unit of work.",
                rekazCustomerId, gymType);

            ((DbContext)_context).ChangeTracker.Clear();

            // Re-run the whole unit of work ONCE
            await _customerSyncService.EnsureLocalCustomerAsync(rekazCustomerId, gymType, ct);

            foreach (var sub in subscriptions)
            {
                await _subscriptionUpsertService.UpsertSubscriptionAsync(sub, gymType, ct);
            }

            await _context.SaveChangesAsync(ct);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        // MySQL error 1062 / Duplicate entry, or SQL Server error 2601 / 2627 / unique constraint
        return message.Contains("1062") ||
               message.Contains("2601") ||
               message.Contains("2627") ||
               message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("UNIQUE key", StringComparison.OrdinalIgnoreCase);
    }
}
