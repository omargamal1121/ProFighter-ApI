using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Jobs;

/// <summary>
/// Background job that syncs subscriptions for a specific user from Rekaz
/// and updates or inserts any missing/outdated subscriptions in the local DB.
/// </summary>
public class UserSubscriptionSyncJob
{
    private readonly IApplicationDbContext _context;
    private readonly IRekazClientFactory _clientFactory;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly ILogger<UserSubscriptionSyncJob> _logger;

    public UserSubscriptionSyncJob(
        IApplicationDbContext context,
        IRekazClientFactory clientFactory,
        ICustomerProvisioningService provisioningService,
        ILogger<UserSubscriptionSyncJob> logger)
    {
        _context = context;
        _clientFactory = clientFactory;
        _provisioningService = provisioningService;
        _logger = logger;
    }

    public async Task SyncUserSubscriptionsAsync(Guid customerId, GymType gymType, CancellationToken ct = default)
    {
        _logger.LogInformation("UserSubscriptionSyncJob starting for CustomerId={CustomerId}, GymType={GymType}", customerId, gymType);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.GymType == gymType, ct);

        if (customer is null || customer.RekazCustomerId is null)
        {
            _logger.LogWarning("UserSubscriptionSyncJob skipped: Customer {CustomerId} not found or not synced with Rekaz for {GymType}", customerId, gymType);
            return;
        }

        var rekazClient = _clientFactory.GetClient(gymType);
        var skipCount = 0;
        const int pageSize = 100;
        var interPageDelay = TimeSpan.FromMilliseconds(50);

        int synced = 0, skipped = 0, failed = 0;

        while (true)
        {
            RekazSubscriptionsListResult result;
            try
            {
                var query = new RekazSubscriptionsQuery(
                    CustomerId: customer.RekazCustomerId,
                    MaxResultCount: pageSize,
                    SkipCount: skipCount);

                result = await rekazClient.Subscriptions.GetSubscriptionsAsync(query, ct);
            }
            catch (ProFighter.Application.Common.Exceptions.RekazApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rekaz rate limit hit (429) for CustomerId={CustomerId} at SkipCount={SkipCount}.", customerId, skipCount);
                await Task.Delay(interPageDelay * 4, ct);
                var query = new RekazSubscriptionsQuery(
                    CustomerId: customer.RekazCustomerId,
                    MaxResultCount: pageSize,
                    SkipCount: skipCount);
                result = await rekazClient.Subscriptions.GetSubscriptionsAsync(query, ct);
            }

            if (result.Items is null || result.Items.Count == 0)
                break;

            foreach (var rekazSub in result.Items)
            {
                try
                {
                    if (rekazSub.TotalAmount < 0)
                    {
                        _logger.LogWarning("Skipping subscription RekazId={SubId}: negative total amount ({TotalAmount})", rekazSub.Id, rekazSub.TotalAmount);
                        skipped++;
                        continue;
                    }

                    var existing = await _context.Subscriptions
                        .FirstOrDefaultAsync(s => s.RekazSubscriptionId == rekazSub.Id && s.GymType == gymType, ct);

                    if (existing is null)
                    {
                        var newSub = new Subscription(
                            id: Guid.NewGuid(),
                            customerId: customer.Id,
                            rekazSubscriptionId: rekazSub.Id,
                            type: SubscriptionType.MartialArts,
                            startDate: rekazSub.StartAt,
                            price: rekazSub.TotalAmount,
                            name: rekazSub.Name,
                            gymType: gymType);

                        newSub.SyncFromRekaz(rekazSub.Status, rekazSub.StartAt, rekazSub.EndAt, rekazSub.TotalAmount, rekazSub.Name);
                        _context.Subscriptions.Add(newSub);
                        _logger.LogInformation("Inserted local subscription {SubId} for RekazSubscriptionId={RekazSubId} (CustomerId={CustomerId})",
                            newSub.Id, rekazSub.Id, customer.Id);
                    }
                    else
                    {
                        existing.SyncFromRekaz(rekazSub.Status, rekazSub.StartAt, rekazSub.EndAt, rekazSub.TotalAmount, rekazSub.Name);
                        _logger.LogInformation("Updated local subscription {SubId} for RekazSubscriptionId={RekazSubId} (CustomerId={CustomerId})",
                            existing.Id, rekazSub.Id, customer.Id);
                    }

                    synced++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "Failed to upsert subscription RekazId={RekazId} for CustomerId={CustomerId}", rekazSub.Id, customer.Id);
                }
            }

            await _context.SaveChangesAsync(ct);
            ((DbContext)_context).ChangeTracker.Clear();

            skipCount += result.Items.Count;

            _logger.LogInformation("Fetched user subscription page starting at {SkipCount}, received {Count}/{TotalCount} items.", 
                skipCount - result.Items.Count, result.Items.Count, result.TotalCount);

            if (skipCount >= result.TotalCount || result.Items.Count == 0)
                break;

            await Task.Delay(interPageDelay, ct);
        }

        _logger.LogInformation("UserSubscriptionSyncJob completed for CustomerId={CustomerId}: synced={Synced}, skipped={Skipped}, failed={Failed}",
            customerId, synced, skipped, failed);
    }
}
