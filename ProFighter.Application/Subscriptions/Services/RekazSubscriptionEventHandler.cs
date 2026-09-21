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

public class RekazSubscriptionEventHandler : IRekazSubscriptionEventHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRekazClientFactory _rekazClientFactory;
    private readonly IRekazCustomerSyncService _customerSyncService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RekazSubscriptionEventHandler> _logger;

    public RekazSubscriptionEventHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IRekazClientFactory rekazClientFactory,
        IRekazCustomerSyncService customerSyncService,
        INotificationService notificationService,
        ILogger<RekazSubscriptionEventHandler> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _rekazClientFactory = rekazClientFactory;
        _customerSyncService = customerSyncService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(Guid rekazSubscriptionId, string eventName, GymType gymType = GymType.ProFighter, CancellationToken ct = default)
    {
        var fetchResult = await FetchSubscriptionAsync(rekazSubscriptionId, gymType, ct);
        if (fetchResult is null)
        {
            _logger.LogWarning("Subscription {RekazSubscriptionId} not found on re-fetch — allowing Hangfire retry", rekazSubscriptionId);
            throw new InvalidOperationException($"Subscription {rekazSubscriptionId} not found in Rekaz - allowing retry mechanism to handle");
        }

        var (fetched, resolvedGymType) = fetchResult.Value;
        var outcomeResult = "Skipped";

        await _unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            var customer = await _customerSyncService.EnsureLocalCustomerAsync(fetched.CustomerId, resolvedGymType, innerCt);
            if (customer is null)
            {
                outcomeResult = "Skipped (CustomerMissing)";
                return true;
            }

            var existingSubscription = _context.Subscriptions.Local
                .FirstOrDefault(s => s.RekazSubscriptionId == rekazSubscriptionId)
                ?? await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.RekazSubscriptionId == rekazSubscriptionId, innerCt);

            Subscription? newlyCreatedSub = null;
            if (existingSubscription is null)
            {
                var newSubscription = new Subscription(
                    id: Guid.NewGuid(),
                    customerId: customer.Id,
                    rekazSubscriptionId: rekazSubscriptionId,
                    type: null, 
                    startDate: fetched.StartAt,
                    price: fetched.TotalAmount,
                    name: fetched.Name,
                    gymType: resolvedGymType);
                newSubscription.SyncFromRekaz(fetched.Status, fetched.StartAt, fetched.EndAt, fetched.TotalAmount, fetched.Name);
                _context.Subscriptions.Add(newSubscription);
                newlyCreatedSub = newSubscription;

                outcomeResult = "Created";
            }
            else
            {
                var previousStatus = existingSubscription.Status;
                existingSubscription.SyncFromRekaz(fetched.Status, fetched.StartAt, fetched.EndAt, fetched.TotalAmount, fetched.Name);
                
                if (previousStatus != existingSubscription.Status)
                {
                    _logger.LogInformation("Updated local subscription {LocalId} (GymType: {GymType}) for Rekaz subscription {RekazId} via webhook event {EventName}. Status changed from {PreviousStatus} to {NewStatus}", 
                        existingSubscription.Id, resolvedGymType, rekazSubscriptionId, eventName, previousStatus, existingSubscription.Status);

                    await TrySendStatusNotification(customer.Id, existingSubscription.Status, innerCt);
                    outcomeResult = "Updated";
                }
                else
                {
                    _logger.LogDebug("Updated local subscription {LocalId} (GymType: {GymType}) for Rekaz subscription {RekazId} via webhook event {EventName} (Status unchanged)", 
                        existingSubscription.Id, resolvedGymType, rekazSubscriptionId, eventName);
                    outcomeResult = "Unchanged";
                }
            }

            await _context.SaveChangesAsync(innerCt);

            if (newlyCreatedSub != null)
            {
                _logger.LogInformation("Created local subscription {LocalId} (GymType: {GymType}) for Rekaz subscription {RekazId} via webhook event {EventName}", 
                    newlyCreatedSub.Id, resolvedGymType, rekazSubscriptionId, eventName);
            }

            return true;
        }, ct);

        _logger.LogInformation("Rekaz webhook event {EventName} for {GymType} processed: RekazSubscriptionId={RekazId}, Result={Result}",
            eventName, resolvedGymType, rekazSubscriptionId, outcomeResult);
    }

    private async Task TrySendStatusNotification(Guid customerId, string newStatus, CancellationToken ct)
    {
        var messages = new Dictionary<string, (string Title, string Body)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Active", ("Subscription Activated", "Your subscription is now active.") },
            { "Activated", ("Subscription Activated", "Your subscription is now active.") },
            { "Expired", ("Subscription Expired", "Your subscription has expired.") },
            { "Cancelled", ("Subscription Cancelled", "Your subscription has been cancelled.") }
        };

        if (messages.TryGetValue(newStatus, out var msg))
        {
            await _notificationService.SendToUserAsync(customerId, msg.Title, msg.Body, null, ct);
        }
    }

    private async Task<(RekazSubscriptionResult Subscription, GymType GymType)?> FetchSubscriptionAsync(Guid rekazSubscriptionId, GymType preferredGymType, CancellationToken ct)
    {
        try
        {
            var client = _rekazClientFactory.GetClient(preferredGymType);
            var fetched = await client.Subscriptions.GetSubscriptionByIdAsync(rekazSubscriptionId, ct);
            if (fetched is not null)
            {
                return (fetched, preferredGymType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch subscription {RekazSubscriptionId} for GymType {GymType}", rekazSubscriptionId, preferredGymType);
        }

        Exception? lastException = null;
        foreach (var fallbackGym in Enum.GetValues<GymType>())
        {
            if (fallbackGym == preferredGymType) continue;
            try
            {
                var client = _rekazClientFactory.GetClient(fallbackGym);
                var fetched = await client.Subscriptions.GetSubscriptionByIdAsync(rekazSubscriptionId, ct);
                if (fetched is not null)
                {
                    return (fetched, fallbackGym);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogDebug(ex, "Failed fallback fetch for subscription {RekazSubscriptionId} on GymType {GymType}", rekazSubscriptionId, fallbackGym);
            }
        }

        if (lastException is not null && lastException is not ProFighter.Application.Common.Exceptions.RekazApiException { StatusCode: System.Net.HttpStatusCode.NotFound })
        {
            _logger.LogWarning(lastException, "Exception fetching subscription {RekazSubscriptionId} across all gyms", rekazSubscriptionId);
        }

        return null;
    }
}
