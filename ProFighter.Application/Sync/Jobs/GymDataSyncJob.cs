using System;
using System.Collections.Generic;
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

            await _context.SaveChangesAsync(ct);
            ((DbContext)_context).ChangeTracker.Clear();

            skipCount += result.Items.Count;

            _logger.LogInformation("Fetched customer page starting at {SkipCount}, received {Count}/{TotalCount} items for {GymType}.", 
                skipCount - result.Items.Count, result.Items.Count, result.TotalCount, gymType);

            if (skipCount >= result.TotalCount || result.Items.Count == 0)
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

    private async Task<(int Synced, int Skipped, int Failed)> SyncSubscriptionsAsync(
        GymType gymType,
        IRekazClient rekazClient,
        CancellationToken ct)
    {
        var synced = 0;
        var skipped = 0;
        var failed = 0;
        var skipCount = 0;

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
                    var itemResult = await _subscriptionUpsertService.UpsertSubscriptionAsync(rekazSub, gymType, ct);
                    if (itemResult == SubscriptionUpsertResult.Created || itemResult == SubscriptionUpsertResult.Updated)
                        synced++;
                    else
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

            await _context.SaveChangesAsync(ct);
            ((DbContext)_context).ChangeTracker.Clear();
            skipCount += result.Items.Count;

            _logger.LogInformation("Fetched page starting at {SkipCount}, received {Count}/{TotalCount} items for {GymType}.", 
                skipCount - result.Items.Count, result.Items.Count, result.TotalCount, gymType);

            if (skipCount >= result.TotalCount || result.Items.Count == 0)
                break;

            await Task.Delay(InterPageDelay, ct);
        }

        _logger.LogInformation(
            "Subscription sync finished for {GymType}: synced={Synced}, skipped={Skipped}, failed={Failed}.",
            gymType, synced, skipped, failed);

        return (synced, skipped, failed);
    }
}
