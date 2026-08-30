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

public class RekazSubscriptionEventHandler : IRekazSubscriptionEventHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRekazSubscriptionsClient _subscriptionsClient;
    private readonly IRekazCustomerSyncService _customerSyncService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RekazSubscriptionEventHandler> _logger;

    public RekazSubscriptionEventHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IRekazSubscriptionsClient subscriptionsClient,
        IRekazCustomerSyncService customerSyncService,
        INotificationService notificationService,
        ILogger<RekazSubscriptionEventHandler> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _subscriptionsClient = subscriptionsClient;
        _customerSyncService = customerSyncService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(Guid rekazSubscriptionId, string eventName, CancellationToken ct)
    {
        var fetched = await _subscriptionsClient.GetSubscriptionByIdAsync(rekazSubscriptionId, ct);
        if (fetched is null)
        {
            _logger.LogWarning("Subscription {RekazSubscriptionId} not found on re-fetch — allowing Hangfire retry", rekazSubscriptionId);
            throw new InvalidOperationException($"Subscription {rekazSubscriptionId} not found in Rekaz - allowing retry mechanism to handle");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            var customer = await _customerSyncService.EnsureLocalCustomerAsync(fetched.CustomerId, innerCt);

            var existingSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.RekazSubscriptionId == rekazSubscriptionId, innerCt);

            if (existingSubscription is null)
            {
                var newSubscription = new Subscription(
                    id: Guid.NewGuid(),
                    customerId: customer.Id,
                    rekazSubscriptionId: rekazSubscriptionId,
                    type: SubscriptionType.MartialArts, 
                    startDate: fetched.StartAt,
                    price: fetched.TotalAmount);
                newSubscription.SyncFromRekaz(fetched.Status, fetched.StartAt, fetched.EndAt, fetched.TotalAmount);
                _context.Subscriptions.Add(newSubscription);
                _logger.LogInformation("Created local subscription {LocalId} for Rekaz subscription {RekazId} via webhook event {EventName}", 
                    newSubscription.Id, rekazSubscriptionId, eventName);
            }
            else
            {
                var previousStatus = existingSubscription.Status;
                existingSubscription.SyncFromRekaz(fetched.Status, fetched.StartAt, fetched.EndAt, fetched.TotalAmount);
                
                if (previousStatus != existingSubscription.Status)
                {
                    _logger.LogInformation("Updated local subscription {LocalId} for Rekaz subscription {RekazId} via webhook event {EventName}. Status changed from {PreviousStatus} to {NewStatus}", 
                        existingSubscription.Id, rekazSubscriptionId, eventName, previousStatus, existingSubscription.Status);

                    await TrySendStatusNotification(customer.Id, existingSubscription.Status, innerCt);
                }
                else
                {
                    _logger.LogInformation("Updated local subscription {LocalId} for Rekaz subscription {RekazId} via webhook event {EventName}", 
                        existingSubscription.Id, rekazSubscriptionId, eventName);
                }
            }

            await _context.SaveChangesAsync(innerCt);
            return true;
        }, ct);
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
}
