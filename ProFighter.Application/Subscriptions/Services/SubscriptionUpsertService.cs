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

namespace ProFighter.Application.Subscriptions.Services;

public class SubscriptionUpsertService : ISubscriptionUpsertService
{
    private readonly IApplicationDbContext _context;
    private readonly IRekazCustomerSyncService _customerSyncService;
    private readonly ILogger<SubscriptionUpsertService> _logger;

    public SubscriptionUpsertService(
        IApplicationDbContext context,
        IRekazCustomerSyncService customerSyncService,
        ILogger<SubscriptionUpsertService> logger)
    {
        _context = context;
        _customerSyncService = customerSyncService;
        _logger = logger;
    }

    public async Task<SubscriptionUpsertResult> UpsertSubscriptionAsync(
        RekazSubscriptionResult rekazSub,
        GymType gymType,
        ISet<Guid>? negativeCache = null,
        ISet<Guid>? unmappedProductTracker = null,
        CancellationToken ct = default)
    {
        if (rekazSub.TotalAmount < 0)
        {
            _logger.LogWarning(
                "Skipping subscription RekazId={SubId} for {GymType}: negative total amount ({TotalAmount}).",
                rekazSub.Id, gymType, rekazSub.TotalAmount);
            return SubscriptionUpsertResult.Skipped;
        }

        var customer = await _customerSyncService.EnsureLocalCustomerAsync(rekazSub.CustomerId, gymType, negativeCache, ct);
        if (customer is null)
        {
            return SubscriptionUpsertResult.Skipped;
        }

        var existing = _context.Subscriptions.Local
            .FirstOrDefault(s => s.RekazSubscriptionId == rekazSub.Id && s.GymType == gymType)
            ?? await _context.Subscriptions
                .FirstOrDefaultAsync(
                    s => s.RekazSubscriptionId == rekazSub.Id && s.GymType == gymType,
                    ct);

        if (existing is null)
        {
            var newSub = new Subscription(
                id: Guid.NewGuid(),
                customerId: customer.Id,
                rekazSubscriptionId: rekazSub.Id,
                type: null,
                startDate: rekazSub.StartAt,
                price: rekazSub.TotalAmount,
                name: rekazSub.Name,
                gymType: gymType);

            newSub.SyncFromRekaz(rekazSub.Status, rekazSub.StartAt, rekazSub.EndAt, rekazSub.TotalAmount, rekazSub.Name);
            _context.Subscriptions.Add(newSub);

            _logger.LogInformation("Created local subscription {SubId} for RekazId={RekazId} ({GymType}, Status={Status})",
                newSub.Id, rekazSub.Id, gymType, rekazSub.Status);

            return SubscriptionUpsertResult.Created;
        }
        else
        {
            var previousStatus = existing.Status;
            var statusChanged = !string.Equals(previousStatus, rekazSub.Status, StringComparison.OrdinalIgnoreCase);

            existing.SyncFromRekaz(rekazSub.Status, rekazSub.StartAt, rekazSub.EndAt, rekazSub.TotalAmount, rekazSub.Name);

            if (statusChanged)
            {
                _logger.LogInformation("Updated local subscription {SubId} for RekazId={RekazId} ({GymType}). Status changed from {PreviousStatus} to {NewStatus}",
                    existing.Id, rekazSub.Id, gymType, previousStatus, existing.Status);
                return SubscriptionUpsertResult.Updated;
            }
            else
            {
                _logger.LogDebug("Updating existing local subscription {SubId} for RekazId={RekazId} ({GymType}, Status={Status})",
                    existing.Id, rekazSub.Id, gymType, existing.Status);
                return SubscriptionUpsertResult.Unchanged;
            }
        }
    }
}
