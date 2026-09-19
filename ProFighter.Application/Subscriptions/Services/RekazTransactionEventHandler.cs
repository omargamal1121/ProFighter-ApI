using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Application.Subscriptions.Jobs;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Services;

public class RekazTransactionEventHandler : IRekazTransactionEventHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IRekazClientFactory _rekazClientFactory;
    private readonly IRekazCustomerSyncService _customerSyncService;
    private readonly INotificationService _notificationService;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<RekazTransactionEventHandler> _logger;

    public RekazTransactionEventHandler(
        IApplicationDbContext context,
        IRekazClientFactory rekazClientFactory,
        IRekazCustomerSyncService customerSyncService,
        INotificationService notificationService,
        IBackgroundJobClient backgroundJobs,
        ILogger<RekazTransactionEventHandler> logger)
    {
        _context = context;
        _rekazClientFactory = rekazClientFactory;
        _customerSyncService = customerSyncService;
        _notificationService = notificationService;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task HandleAsync(Guid transactionId, string eventName, CancellationToken ct)
    {
        var fetchResult = await FetchTransactionAsync(transactionId, ct);
        if (fetchResult is null)
        {
            _logger.LogWarning("Transaction {TransactionId} not found on re-fetch — allowing Hangfire retry", transactionId);
            throw new InvalidOperationException($"Transaction {transactionId} not found in Rekaz - allowing retry mechanism to handle");
        }

        var (fetched, gymType) = fetchResult.Value;

        // Ensure customer exists locally using explicit GymType
        var customer = await _customerSyncService.EnsureLocalCustomerAsync(fetched.CustomerId, gymType, ct);

        // Check if transaction contains at least one item of type "Subscription" (or with product/price ID)
        var subscriptionItems = fetched.Items?
            .Where(i => string.Equals(i.Type, "Subscription", StringComparison.OrdinalIgnoreCase) ||
                        (string.IsNullOrWhiteSpace(i.Type) && (i.ProductId.HasValue || i.PriceId.HasValue)))
            .ToList();

        if (subscriptionItems != null && subscriptionItems.Count > 0)
        {
            var productIds = subscriptionItems
                .Select(i => i.ProductId ?? i.PriceId ?? Guid.Empty)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            var createdAtUtc = fetched.CreationTime ?? DateTimeOffset.UtcNow;

            _logger.LogInformation("Enqueuing SyncCustomerSubscriptionsJob for {GymType}, CustomerId={CustomerId}, TransactionId={TransactionId}",
                gymType, fetched.CustomerId, fetched.Id);

            _backgroundJobs.Enqueue<SyncCustomerSubscriptionsJob>(j =>
                j.ExecuteAsync(gymType, fetched.CustomerId, createdAtUtc, productIds, null, CancellationToken.None));
        }
        else
        {
            _logger.LogInformation("Transaction {TransactionId} contains no Subscription items (items count: {Count}). Skipping subscription sync job enqueue.",
                fetched.Id, fetched.Items?.Count ?? 0);
        }

        // Dedup marker: check if we already processed this specific transaction ID for notifications
        var alreadyProcessed = await _context.RekazWebhookInboxEntries
            .AnyAsync(e => e.Id == fetched.Id && e.EventName == "TransactionPaidNotification", ct);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Transaction {TransactionId} was already processed for notifications. Skipping.", fetched.Id);
            return;
        }

        // Processing rule: paymentStatus == "Paid", optionally status == "Confirmed", remainingAmount == 0
        if (fetched.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase) && 
            fetched.RemainingAmount == 0)
        {
            var dataPayload = new Dictionary<string, string>
            {
                { "transactionId", fetched.Id.ToString() },
                { "paidAmount", fetched.PaidAmount.ToString("F2") },
                { "currency", fetched.Currency }
            };

            var itemName = fetched.Items?.FirstOrDefault()?.NameAr ?? fetched.Items?.FirstOrDefault()?.NameEn ?? "الاشتراك";
            var body = $"تم دفع {fetched.PaidAmount} {fetched.Currency} لـ {itemName}";

            await _notificationService.SendToUserAsync(
                customer.Id, 
                "Payment Received / تم استلام الدفعة", 
                body, 
                dataPayload, 
                ct);

            // Insert dedup marker
            var marker = new ProFighter.Domain.Entities.RekazWebhookInboxEntry(
                fetched.Id, 
                "TransactionPaidNotification", 
                "{}"
            );
            marker.MarkProcessed();
            _context.RekazWebhookInboxEntries.Add(marker);
            await _context.SaveChangesAsync(ct);
        }
        
        _logger.LogInformation("Processed transaction {TransactionId} via webhook event {EventName}", transactionId, eventName);
    }

    private async Task<(RekazTransactionResult Transaction, GymType GymType)?> FetchTransactionAsync(Guid transactionId, CancellationToken ct)
    {
        Exception? lastException = null;
        foreach (var gymType in Enum.GetValues<GymType>())
        {
            try
            {
                var client = _rekazClientFactory.GetClient(gymType);
                var fetched = await client.Transactions.GetTransactionByIdAsync(transactionId, ct);
                if (fetched is not null)
                {
                    return (fetched, gymType);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogDebug(ex, "Failed to fetch transaction {TransactionId} for GymType {GymType}", transactionId, gymType);
            }
        }

        if (lastException is not null && lastException is not ProFighter.Application.Common.Exceptions.RekazApiException { StatusCode: System.Net.HttpStatusCode.NotFound })
        {
            _logger.LogWarning(lastException, "Exception fetching transaction {TransactionId} across all gyms", transactionId);
        }

        return null;
    }
}
