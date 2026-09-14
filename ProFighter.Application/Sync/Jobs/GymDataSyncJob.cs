using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Sync.Jobs;

/// <summary>
/// Hangfire job that syncs customers and subscriptions from Rekaz for each gym.
///
/// Two separate public entry points allow Hangfire to schedule them independently,
/// so a failure in one gym's sync cannot block or delay the other.
///
/// Registration (in Program.cs — reuses the existing Hangfire setup):
///
///   RecurringJob.AddOrUpdate&lt;GymDataSyncJob&gt;(
///       "gym-data-sync-profighter",
///       job => job.SyncProFighterAsync(),
///       "0 3 * * *",
///       new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time") });
///
///   RecurringJob.AddOrUpdate&lt;GymDataSyncJob&gt;(
///       "gym-data-sync-progym",
///       job => job.SyncProGymAsync(),
///       "30 3 * * *",
///       new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time") });
/// </summary>
public sealed class GymDataSyncJob
{
    // TODO: Confirm this delay value against Rekaz's actual documented rate limit (if any).
    // Currently set to 200 ms between pages as a conservative default.
    private static readonly TimeSpan InterPageDelay = TimeSpan.FromMilliseconds(50);

    private const int PageSize = 100;

    private readonly IRekazClientFactory _clientFactory;
    private readonly IApplicationDbContext _context;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly ILogger<GymDataSyncJob> _logger;

    public GymDataSyncJob(
        IRekazClientFactory clientFactory,
        IApplicationDbContext context,
        ICustomerProvisioningService provisioningService,
        ILogger<GymDataSyncJob> logger)
    {
        _clientFactory       = clientFactory;
        _context             = context;
        _provisioningService = provisioningService;
        _logger              = logger;
    }

    private enum SyncItemResult
    {
        Synced,
        Skipped,
        Failed
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public Hangfire entry points — one per gym
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Hangfire entry point: sync Pro Fighter gym (all).</summary>
    public Task SyncProFighterAsync(CancellationToken ct = default)
        => SyncGymAsync(GymType.ProFighter, ct);

    /// <summary>Hangfire entry point: sync Pro Gym (all).</summary>
    public Task SyncProGymAsync(CancellationToken ct = default)
        => SyncGymAsync(GymType.ProGym, ct);

    /// <summary>Hangfire entry point: sync Pro Fighter Customers only.</summary>
    public async Task SyncProFighterCustomersAsync(CancellationToken ct = default)
    {
        var rekazClient = _clientFactory.GetClient(GymType.ProFighter);
        await SyncCustomersAsync(GymType.ProFighter, rekazClient, ct);
    }

    /// <summary>Hangfire entry point: sync Pro Fighter Subscriptions only.</summary>
    public async Task SyncProFighterSubscriptionsAsync(CancellationToken ct = default)
    {
        var rekazClient = _clientFactory.GetClient(GymType.ProFighter);
        await SyncSubscriptionsAsync(GymType.ProFighter, rekazClient, ct);
    }

    /// <summary>Hangfire entry point: sync Pro Gym Customers only.</summary>
    public async Task SyncProGymCustomersAsync(CancellationToken ct = default)
    {
        var rekazClient = _clientFactory.GetClient(GymType.ProGym);
        await SyncCustomersAsync(GymType.ProGym, rekazClient, ct);
    }

    /// <summary>Hangfire entry point: sync Pro Gym Subscriptions only.</summary>
    public async Task SyncProGymSubscriptionsAsync(CancellationToken ct = default)
    {
        var rekazClient = _clientFactory.GetClient(GymType.ProGym);
        await SyncSubscriptionsAsync(GymType.ProGym, rekazClient, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared sync logic — parameterized by gymType
    // ─────────────────────────────────────────────────────────────────────────

    private async Task SyncGymAsync(GymType gymType, CancellationToken ct)
    {
        _logger.LogInformation("GymDataSyncJob starting for {GymType}", gymType);
        var started = DateTime.UtcNow;

        try
        {
            var rekazClient = _clientFactory.GetClient(gymType);

            var customerStats     = await SyncCustomersAsync(gymType, rekazClient, ct);
            var subscriptionStats = await SyncSubscriptionsAsync(gymType, rekazClient, ct);

            var elapsed = DateTime.UtcNow - started;
            _logger.LogInformation(
                "GymDataSyncJob completed for {GymType} in {Elapsed:g}. " +
                "Customers — synced: {CSync}, skipped: {CSkip}, failed: {CFail}. " +
                "Subscriptions — synced: {SSync}, skipped: {SSkip}, failed: {SFail}.",
                gymType, elapsed,
                customerStats.Synced, customerStats.Skipped, customerStats.Failed,
                subscriptionStats.Synced, subscriptionStats.Skipped, subscriptionStats.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GymDataSyncJob failed for {GymType} after {Elapsed:g}.",
                gymType, DateTime.UtcNow - started);

            // Rethrow so Hangfire marks this job as failed and retries independently.
            // The other gym's job is completely unaffected.
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Customer sync
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<(int Synced, int Skipped, int Failed)> SyncCustomersAsync(
        GymType gymType,
        IRekazClient rekazClient,
        CancellationToken ct)
    {
        var synced = 0;
        var skipped = 0;
        var failed = 0;
        var skipCount = 0;

        _logger.LogInformation("Syncing customers for {GymType}...", gymType);

        while (true)
        {
            RekazCustomersListResult result;
            try
            {
                var query  = new RekazCustomersQuery(MaxResultCount: PageSize, SkipCount: skipCount);
                result = await rekazClient.Customers.GetCustomersAsync(query, ct);
            }
            catch (ProFighter.Application.Common.Exceptions.RekazApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rekaz rate limit hit (429) for {GymType} at SkipCount={SkipCount}. Consider increasing InterPageDelay.", gymType, skipCount);
                await Task.Delay(InterPageDelay * 4, ct);
                var query  = new RekazCustomersQuery(MaxResultCount: PageSize, SkipCount: skipCount);
                result = await rekazClient.Customers.GetCustomersAsync(query, ct);
            }

            if (result.Items.Count == 0)
                break;

            foreach (var rekazCustomer in result.Items)
            {
                try
                {
                    var itemResult = await UpsertCustomerAsync(rekazCustomer, gymType, ct);
                    if (itemResult == SyncItemResult.Synced)
                        synced++;
                    else if (itemResult == SyncItemResult.Skipped)
                        skipped++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex,
                        "Failed to upsert customer RekazId={RekazId} ({Name}) for {GymType}.",
                        rekazCustomer.Id, rekazCustomer.Name, gymType);
                }
            }

            // Save once per page — not once per record.
            await _context.SaveChangesAsync(ct);
            ((DbContext)_context).ChangeTracker.Clear();

            skipCount += PageSize;

            // Last page: stop early instead of making an extra empty-page round-trip.
            if (result.Items.Count < PageSize)
                break;

            await Task.Delay(InterPageDelay, ct);
        }

        _logger.LogInformation(
            "Customer sync finished for {GymType}: synced={Synced}, skipped={Skipped}, failed={Failed}.",
            gymType, synced, skipped, failed);

        return (synced, skipped, failed);
    }

    private async Task<SyncItemResult> UpsertCustomerAsync(
        RekazCustomerResult rekazCustomer,
        GymType gymType,
        CancellationToken ct)
    {
        // Task 1: Check if mobile number is missing/empty/whitespace
        if (string.IsNullOrWhiteSpace(rekazCustomer.MobileNumber))
        {
            _logger.LogWarning(
                "Skipping customer RekazId={RekazId} ({Name}) for {GymType}: missing or empty mobile number.",
                rekazCustomer.Id, rekazCustomer.Name, gymType);
            return SyncItemResult.Skipped;
        }

        var existing = await _context.Customers
            .FirstOrDefaultAsync(
                c => c.RekazCustomerId == rekazCustomer.Id && c.GymType == gymType,
                ct);

        if (existing is null)
        {
            await _provisioningService.ProvisionLocalCustomerAsync(
                rekazCustomer.Id,
                rekazCustomer.Name,
                rekazCustomer.MobileNumber,
                rekazCustomer.Email,
                CustomerSource.LegacyRekazImport,
                gymType,
                ct);
        }
        else
        {
            if (existing.Name        != rekazCustomer.Name        ||
                existing.MobileNumber != rekazCustomer.MobileNumber ||
                existing.Email        != rekazCustomer.Email)
            {
                existing.UpdateProfile(rekazCustomer.Name, rekazCustomer.MobileNumber, rekazCustomer.Email);
            }
        }

        return SyncItemResult.Synced;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Subscription sync
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<(int Synced, int Skipped, int Failed)> SyncSubscriptionsAsync(
        GymType gymType,
        IRekazClient rekazClient,
        CancellationToken ct)
    {
        var synced = 0;
        var skipped = 0;
        var failed = 0;
        var skipCount = 0;

        // Task 2: In-memory set of known 404 customer IDs scoped to this gym's subscription sync run
        var knownMissingCustomers = new HashSet<Guid>();

        _logger.LogInformation("Syncing subscriptions for {GymType}...", gymType);

        while (true)
        {
            RekazSubscriptionsListResult result;
            try
            {
                var query  = new RekazSubscriptionsQuery(MaxResultCount: PageSize, SkipCount: skipCount);
                result = await rekazClient.Subscriptions.GetSubscriptionsAsync(query, ct);
            }
            catch (ProFighter.Application.Common.Exceptions.RekazApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rekaz rate limit hit (429) for {GymType} at SkipCount={SkipCount}. Consider increasing InterPageDelay.", gymType, skipCount);
                await Task.Delay(InterPageDelay * 4, ct);
                var query  = new RekazSubscriptionsQuery(MaxResultCount: PageSize, SkipCount: skipCount);
                result = await rekazClient.Subscriptions.GetSubscriptionsAsync(query, ct);
            }

            if (result.Items.Count == 0)
                break;

            foreach (var rekazSub in result.Items)
            {
                try
                {
                    var itemResult = await UpsertSubscriptionAsync(rekazSub, gymType, rekazClient, knownMissingCustomers, ct);
                    if (itemResult == SyncItemResult.Synced)
                        synced++;
                    else if (itemResult == SyncItemResult.Skipped)
                        skipped++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex,
                        "Failed to upsert subscription RekazId={RekazId} for {GymType}.",
                        rekazSub.Id, gymType);
                }
            }

            // Save once per page.
            await _context.SaveChangesAsync(ct);
            ((DbContext)_context).ChangeTracker.Clear();

            skipCount += PageSize;

            if (result.Items.Count < PageSize)
                break;

            await Task.Delay(InterPageDelay, ct);
        }

        _logger.LogInformation(
            "Subscription sync finished for {GymType}: synced={Synced}, skipped={Skipped}, failed={Failed}.",
            gymType, synced, skipped, failed);

        return (synced, skipped, failed);
    }

    private async Task<SyncItemResult> UpsertSubscriptionAsync(
        RekazSubscriptionResult rekazSub,
        GymType gymType,
        IRekazClient rekazClient,
        HashSet<Guid> knownMissingCustomers,
        CancellationToken ct)
    {
        // Task 3: Treat negative subscription price as skip
        if (rekazSub.TotalAmount < 0)
        {
            _logger.LogWarning(
                "Skipping subscription RekazId={SubId} for {GymType}: negative total amount ({TotalAmount}).",
                rekazSub.Id, gymType, rekazSub.TotalAmount);
            return SyncItemResult.Skipped;
        }

        var existing = await _context.Subscriptions
            .FirstOrDefaultAsync(
                s => s.RekazSubscriptionId == rekazSub.Id && s.GymType == gymType,
                ct);

        if (existing is null)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(
                    c => c.RekazCustomerId == rekazSub.CustomerId && c.GymType == gymType,
                    ct);

            if (customer is null)
            {
                // Task 2: Check known 404 cache before making HTTP request
                if (knownMissingCustomers.Contains(rekazSub.CustomerId))
                {
                    _logger.LogWarning(
                        "Skipping subscription RekazId={SubId}: customer RekazId={CustomerId} previously confirmed 404 in Rekaz for {GymType}.",
                        rekazSub.Id, rekazSub.CustomerId, gymType);
                    return SyncItemResult.Skipped;
                }

                var rekazCustomer = await rekazClient.Customers.GetCustomerByIdAsync(rekazSub.CustomerId, ct);
                if (rekazCustomer is null)
                {
                    // Task 2: Cache the 404 customer ID for this sync run
                    knownMissingCustomers.Add(rekazSub.CustomerId);

                    _logger.LogWarning(
                        "Skipping subscription RekazId={SubId}: customer RekazId={CustomerId} not found in Rekaz (404) for {GymType}.",
                        rekazSub.Id, rekazSub.CustomerId, gymType);
                    return SyncItemResult.Skipped;
                }

                // Task 1: Check if fetched customer has missing/empty mobile number
                if (string.IsNullOrWhiteSpace(rekazCustomer.MobileNumber))
                {
                    _logger.LogWarning(
                        "Skipping subscription RekazId={SubId}: customer RekazId={CustomerId} ({Name}) has missing/empty mobile number for {GymType}.",
                        rekazSub.Id, rekazCustomer.Id, rekazCustomer.Name, gymType);
                    return SyncItemResult.Skipped;
                }

                customer = await _provisioningService.ProvisionLocalCustomerAsync(
                    rekazCustomer.Id,
                    rekazCustomer.Name,
                    rekazCustomer.MobileNumber,
                    rekazCustomer.Email,
                    CustomerSource.LegacyRekazImport,
                    gymType,
                    ct);
            }

            var newSub = new Subscription(
                id:                  Guid.NewGuid(),
                customerId:          customer.Id,
                rekazSubscriptionId: rekazSub.Id,
                type:                SubscriptionType.MartialArts,
                startDate:           rekazSub.StartAt,
                price:               rekazSub.TotalAmount,
                name:                rekazSub.Name,
                gymType:             gymType);

            newSub.SyncFromRekaz(rekazSub.Status, rekazSub.StartAt, rekazSub.EndAt, rekazSub.TotalAmount, rekazSub.Name);
            _context.Subscriptions.Add(newSub);
        }
        else
        {
            existing.SyncFromRekaz(rekazSub.Status, rekazSub.StartAt, rekazSub.EndAt, rekazSub.TotalAmount, rekazSub.Name);
        }

        return SyncItemResult.Synced;
    }
}
