using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Sync.Jobs;

/// <summary>
/// Hangfire job that syncs customers and subscriptions from Rekaz for each gym.
/// </summary>
public sealed class GymDataSyncJob
{
    private static readonly TimeSpan InterPageDelay = TimeSpan.FromMilliseconds(50);
    private const int PageSize = 100;

    private readonly IRekazClientFactory _clientFactory;
    private readonly IApplicationDbContext _context;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly ISubscriptionUpsertService _subscriptionUpsertService;
    private readonly ILogger<GymDataSyncJob> _logger;

    public GymDataSyncJob(
        IRekazClientFactory clientFactory,
        IApplicationDbContext context,
        ICustomerProvisioningService provisioningService,
        ISubscriptionUpsertService subscriptionUpsertService,
        ILogger<GymDataSyncJob> logger)
    {
        _clientFactory          = clientFactory;
        _context                = context;
        _provisioningService    = provisioningService;
        _subscriptionUpsertService = subscriptionUpsertService;
        _logger                 = logger;
    }

    private enum SyncItemResult
    {
        Synced,
        Skipped,
        Failed
    }

    public Task SyncProFighterAsync(CancellationToken ct = default)
        => SyncGymAsync(GymType.ProFighter, ct);

    public Task SyncProGymAsync(CancellationToken ct = default)
        => SyncGymAsync(GymType.ProGym, ct);

    public async Task SyncProFighterCustomersAsync(CancellationToken ct = default)
    {
        var rekazClient = _clientFactory.GetClient(GymType.ProFighter);
        await SyncCustomersAsync(GymType.ProFighter, rekazClient, ct);
    }

    public async Task SyncProFighterSubscriptionsAsync(CancellationToken ct = default)
    {
        var rekazClient = _clientFactory.GetClient(GymType.ProFighter);
        await SyncSubscriptionsAsync(GymType.ProFighter, rekazClient, ct);
    }

    public async Task SyncProGymCustomersAsync(CancellationToken ct = default)
    {
        var rekazClient = _clientFactory.GetClient(GymType.ProGym);
        await SyncCustomersAsync(GymType.ProGym, rekazClient, ct);
    }

    public async Task SyncProGymSubscriptionsAsync(CancellationToken ct = default)
    {
        var rekazClient = _clientFactory.GetClient(GymType.ProGym);
        await SyncSubscriptionsAsync(GymType.ProGym, rekazClient, ct);
    }

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
                "Subscriptions — created: {SCreated}, updated: {SUpdated}, unchanged: {SUnchanged}, skipped: {SSkip}, failed: {SFail}.",
                gymType, elapsed,
                customerStats.Synced, customerStats.Skipped, customerStats.Failed,
                subscriptionStats.Created, subscriptionStats.Updated, subscriptionStats.Unchanged, subscriptionStats.Skipped, subscriptionStats.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GymDataSyncJob failed for {GymType} after {Elapsed:g}.",
                gymType, DateTime.UtcNow - started);

            throw;
        }
    }

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
                _logger.LogWarning("Rekaz rate limit hit (429) for {GymType} at SkipCount={SkipCount}. Retrying after delay.", gymType, skipCount);
                await Task.Delay(InterPageDelay * 4, ct);
                var query  = new RekazCustomersQuery(MaxResultCount: PageSize, SkipCount: skipCount);
                result = await rekazClient.Customers.GetCustomersAsync(query, ct);
            }

            if (result.Items == null || result.Items.Count == 0)
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
                        "Failed to upsert customer RekazId={RekazId} for {GymType}.",
                        rekazCustomer.Id, gymType);
                }
            }

            await _context.SaveChangesAsync(ct);
            ((DbContext)_context).ChangeTracker.Clear();

            skipCount += PageSize;

            _logger.LogDebug("Fetched customer page starting at {SkipCount}, received {Count}/{TotalCount} items for {GymType}.", 
                skipCount - PageSize, result.Items.Count, result.TotalCount, gymType);

            if (skipCount >= result.TotalCount)
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
        if (string.IsNullOrWhiteSpace(rekazCustomer.MobileNumber))
        {
            _logger.LogWarning(
                "Skipping customer RekazId={RekazId} for {GymType}: missing or empty mobile number.",
                rekazCustomer.Id, gymType);
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

    private async Task<(int Created, int Updated, int Unchanged, int Skipped, int Failed)> SyncSubscriptionsAsync(
        GymType gymType,
        IRekazClient rekazClient,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var createdCount = 0;
        var updatedCount = 0;
        var unchangedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;
        var skipCount = 0;
        var totalCount = 0;

        var distinctVisible = new HashSet<Guid>();
        var unmappedProductIds = new HashSet<Guid>();
        var negativeCache = new HashSet<Guid>();

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
                _logger.LogWarning("Rekaz rate limit hit (429) for {GymType} at SkipCount={SkipCount}. Retrying after delay.", gymType, skipCount);
                await Task.Delay(InterPageDelay * 4, ct);
                var query  = new RekazSubscriptionsQuery(MaxResultCount: PageSize, SkipCount: skipCount);
                result = await rekazClient.Subscriptions.GetSubscriptionsAsync(query, ct);
            }

            if (totalCount == 0 && result.TotalCount > 0)
            {
                totalCount = (int)result.TotalCount;
            }

            if (result.Items == null || result.Items.Count == 0)
                break;

            foreach (var rekazSub in result.Items)
            {
                distinctVisible.Add(rekazSub.Id);
                try
                {
                    var itemResult = await _subscriptionUpsertService.UpsertSubscriptionAsync(
                        rekazSub,
                        gymType,
                        negativeCache,
                        unmappedProductIds,
                        ct);

                    if (itemResult == SubscriptionUpsertResult.Created)
                        createdCount++;
                    else if (itemResult == SubscriptionUpsertResult.Updated)
                        updatedCount++;
                    else if (itemResult == SubscriptionUpsertResult.Unchanged)
                        unchangedCount++;
                    else
                        skippedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogError(ex,
                        "Failed to upsert subscription RekazId={RekazId} for {GymType}.",
                        rekazSub.Id, gymType);
                }
            }

            await _context.SaveChangesAsync(ct);
            ((DbContext)_context).ChangeTracker.Clear();

            skipCount += PageSize;

            _logger.LogDebug("Fetched subscription page starting at {SkipCount}, received {Count}/{TotalCount} items for {GymType}.", 
                skipCount - PageSize, result.Items.Count, result.TotalCount, gymType);

            if (skipCount >= result.TotalCount)
                break;

            await Task.Delay(InterPageDelay, ct);
        }

        sw.Stop();
        var visibleCount = distinctVisible.Count;
        var hiddenCount = Math.Max(0, totalCount - visibleCount);

        _logger.LogInformation(
            "Subscription sync completed for {GymType} in {Elapsed:g}: totalCount={TotalCount}, visible={Visible}, hidden={Hidden}, created={Created}, updated={Updated}, unchanged={Unchanged}, skipped={Skipped}, failed={Failed}, distinctUnmappedProducts={DistinctUnmappedProducts}",
            gymType, sw.Elapsed, totalCount, visibleCount, hiddenCount, createdCount, updatedCount, unchangedCount, skippedCount, failedCount, unmappedProductIds.Count);

        return (createdCount, updatedCount, unchangedCount, skippedCount, failedCount);
    }
}
