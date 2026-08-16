using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Application.Subscriptions.Common;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;
using System.Text.Json;

namespace ProFighter.Application.Subscriptions.Services;

public class RekazWebhookProcessor : IRekazWebhookProcessor
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRekazSubscriptionsClient _subscriptionsClient;
    private readonly IRekazCustomersClient _customersClient;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly ILogger<RekazWebhookProcessor> _logger;

    public RekazWebhookProcessor(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IRekazSubscriptionsClient subscriptionsClient,
        IRekazCustomersClient customersClient,
        ICustomerProvisioningService provisioningService,
        ILogger<RekazWebhookProcessor> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _subscriptionsClient = subscriptionsClient;
        _customersClient = customersClient;
        _provisioningService = provisioningService;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid webhookEventId, CancellationToken ct = default)
    {
        var entry = await _context.RekazWebhookInboxEntries.FirstOrDefaultAsync(w => w.Id == webhookEventId, ct);
        if (entry is null || entry.Processed) return; // already handled or somehow missing

        var payload = JsonSerializer.Deserialize<JsonElement>(entry.RawPayload);
        var dataId = Guid.Parse(payload.GetProperty("Data").GetProperty("Id").GetString()!);

        if (entry.EventName.StartsWith("Subscription", StringComparison.Ordinal))
        {
            await ProcessSubscriptionEventAsync(dataId, ct);
        }
        else
        {
            // Reservation/Merchandise/Gift events: this project currently relies only on
            // Subscriptions (Reservations were explicitly deprioritized earlier). Record
            // the event for future use but take no further action — the unique-Id row
            // already proves we saw it, per Rekaz's own guidance to store Id before
            // applying side effects.
            _logger.LogInformation("Rekaz webhook {EventName} ({EventId}) recorded, no handler implemented yet.", entry.EventName, webhookEventId);
        }

        entry.MarkProcessed();
        await _context.SaveChangesAsync(ct);
    }

    private async Task ProcessSubscriptionEventAsync(Guid rekazSubscriptionId, CancellationToken ct)
    {
        // Re-fetch pattern: NEVER trust the webhook payload's embedded state — Rekaz's own
        // security docs mandate re-reading the resource through the authenticated API
        // before applying any side effect.
        var fetched = await _subscriptionsClient.GetSubscriptionByIdAsync(rekazSubscriptionId, ct);
        if (fetched is null)
        {
            _logger.LogWarning("Subscription {RekazSubscriptionId} not found on re-fetch — skipping sync.", rekazSubscriptionId);
            return;
        }

        await _unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.RekazCustomerId == fetched.CustomerId, innerCt);

            if (customer is null)
            {
                // Subscription references a customer we don't know locally yet — likely
                // created via a channel outside our app (e.g. cashier using Rekaz's own
                // dashboard directly, as discussed). Fetch and provision that customer now
                // so the subscription has a valid local FK target.
                var rekazCustomer = await _customersClient.GetCustomerByIdAsync(fetched.CustomerId, innerCt)
                    ?? throw new InvalidOperationException($"Rekaz customer {fetched.CustomerId} referenced by subscription {rekazSubscriptionId} could not be found via Rekaz API.");

                var newCustomerId = await _provisioningService.ProvisionLocalCustomerAsync(
                    rekazCustomer.Id, rekazCustomer.Name, rekazCustomer.MobileNumber, rekazCustomer.Email,
                    CustomerSource.LegacyRekazImport, innerCt);

                customer = await _context.Customers.FirstAsync(c => c.Id == newCustomerId, innerCt);
            }

            var localStatus = RekazSubscriptionStatusMapper.Map(fetched.Status);
            var existingSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.RekazSubscriptionId == rekazSubscriptionId, innerCt);

            if (existingSubscription is null)
            {
                // TODO: mapping a Rekaz priceId to our local SubscriptionType (MartialArts/Swimming)
                // requires a priceId → SubscriptionType lookup not yet built. Defaulting to
                // MartialArts and flagging for review — do NOT block the sync on this, since
                // payment-status tracking (the primary reason this project cares about
                // subscriptions) doesn't depend on Type being exactly right.
                var newSubscription = new Subscription(
                    id: Guid.NewGuid(),
                    customerId: customer.Id,
                    rekazSubscriptionId: rekazSubscriptionId,
                    type: SubscriptionType.MartialArts, // TODO: real priceId→Type mapping
                    startDate: fetched.StartAt,
                    price: fetched.TotalAmount);
                _context.Subscriptions.Add(newSubscription);
            }
            else
            {
                existingSubscription.SyncFromRekaz(localStatus, fetched.StartAt, fetched.EndAt, fetched.TotalAmount);
            }

            await _context.SaveChangesAsync(innerCt);
            return true;
        }, ct);
    }
}
