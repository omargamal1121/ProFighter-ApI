using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Application.Subscriptions.Jobs;

/// <summary>
/// One-time backfill job that iterates over all existing local subscriptions with a NULL Name,
/// fetches their details from Rekaz by ID, and populates the Name field.
/// </summary>
public class SubscriptionNameBackfillJob
{
    private readonly IApplicationDbContext _context;
    private readonly IRekazSubscriptionsClient _subscriptionsClient;
    private readonly ILogger<SubscriptionNameBackfillJob> _logger;

    public SubscriptionNameBackfillJob(
        IApplicationDbContext context,
        IRekazSubscriptionsClient subscriptionsClient,
        ILogger<SubscriptionNameBackfillJob> logger)
    {
        _context = context;
        _subscriptionsClient = subscriptionsClient;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting SubscriptionNameBackfillJob...");

        var subscriptionsToBackfill = await _context.Subscriptions
            .Where(s => s.Name == null)
            .ToListAsync(ct);

        _logger.LogInformation("Found {Count} subscriptions with NULL Name to backfill.", subscriptionsToBackfill.Count);

        int updatedCount = 0;
        int skippedCount = 0;
        int errorCount = 0;

        foreach (var subscription in subscriptionsToBackfill)
        {
            try
            {
                var fetched = await _subscriptionsClient.GetSubscriptionByIdAsync(subscription.RekazSubscriptionId, ct);
                if (fetched != null && !string.IsNullOrWhiteSpace(fetched.Name))
                {
                    subscription.SetName(fetched.Name);
                    updatedCount++;
                    _logger.LogDebug("Backfilled subscription {Id} with name: {Name}", subscription.Id, fetched.Name);
                }
                else
                {
                    skippedCount++;
                    _logger.LogDebug("Subscription {Id} (Rekaz ID {RekazId}) returned no name from Rekaz.", 
                        subscription.Id, subscription.RekazSubscriptionId);
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogWarning(ex, "Failed to backfill name for subscription {Id} (Rekaz ID {RekazId})", 
                    subscription.Id, subscription.RekazSubscriptionId);
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Completed SubscriptionNameBackfillJob. Total: {Total}, Updated: {Updated}, Skipped: {Skipped}, Errors: {Errors}",
            subscriptionsToBackfill.Count, updatedCount, skippedCount, errorCount);
    }
}
