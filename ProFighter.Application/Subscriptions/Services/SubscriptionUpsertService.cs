using System;
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
    private readonly ISubscriptionTypeMapper _typeMapper;
    private readonly ILogger<SubscriptionUpsertService> _logger;

    public SubscriptionUpsertService(
        IApplicationDbContext context,
        IRekazCustomerSyncService customerSyncService,
        ISubscriptionTypeMapper typeMapper,
        ILogger<SubscriptionUpsertService> logger)
    {
        _context = context;
        _customerSyncService = customerSyncService;
        _typeMapper = typeMapper;
        _logger = logger;
    }

    public async Task<SubscriptionUpsertResult> UpsertSubscriptionAsync(
        RekazSubscriptionResult rekazSub,
        GymType gymType,
        CancellationToken ct = default)
    {
        if (rekazSub.TotalAmount < 0)
        {
            _logger.LogWarning(
                "Skipping subscription RekazId={SubId} for {GymType}: negative total amount ({TotalAmount}).",
                rekazSub.Id, gymType, rekazSub.TotalAmount);
            return SubscriptionUpsertResult.Skipped;
        }

        var existing = await _context.Subscriptions
            .FirstOrDefaultAsync(
                s => s.RekazSubscriptionId == rekazSub.Id && s.GymType == gymType,
                ct);

        var subscriptionType = _typeMapper.MapSubscriptionType(gymType, rekazSub.ProductId ?? rekazSub.PriceId);

        if (existing is null)
        {
            var customer = await _customerSyncService.EnsureLocalCustomerAsync(rekazSub.CustomerId, gymType, ct);

            var newSub = new Subscription(
                id: Guid.NewGuid(),
                customerId: customer.Id,
                rekazSubscriptionId: rekazSub.Id,
                type: subscriptionType,
                startDate: rekazSub.StartAt,
                price: rekazSub.TotalAmount,
                name: rekazSub.Name,
                gymType: gymType);

            newSub.SyncFromRekaz(rekazSub.Status, rekazSub.StartAt, rekazSub.EndAt, rekazSub.TotalAmount, rekazSub.Name, subscriptionType);
            _context.Subscriptions.Add(newSub);

            _logger.LogInformation("Creating new local subscription {SubId} for RekazId={RekazId} ({GymType}, Type={Type}, Status={Status})",
                newSub.Id, rekazSub.Id, gymType, subscriptionType, rekazSub.Status);

            return SubscriptionUpsertResult.Created;
        }
        else
        {
            existing.SyncFromRekaz(rekazSub.Status, rekazSub.StartAt, rekazSub.EndAt, rekazSub.TotalAmount, rekazSub.Name, subscriptionType);

            _logger.LogInformation("Updating existing local subscription {SubId} for RekazId={RekazId} ({GymType}, Type={Type}, Status={Status})",
                existing.Id, rekazSub.Id, gymType, subscriptionType, rekazSub.Status);

            return SubscriptionUpsertResult.Updated;
        }
    }
}
