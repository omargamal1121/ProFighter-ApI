using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Commands.CreateSubscription;

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, CreateSubscriptionResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRekazSubscriptionsClient _rekazSubscriptionsClient;
    private readonly ILogger<CreateSubscriptionCommandHandler> _logger;

    public CreateSubscriptionCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IRekazSubscriptionsClient rekazSubscriptionsClient,
        ILogger<CreateSubscriptionCommandHandler> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _rekazSubscriptionsClient = rekazSubscriptionsClient;
        _logger = logger;
    }

    public async Task<CreateSubscriptionResult> Handle(CreateSubscriptionCommand request, CancellationToken ct)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct)
            ?? throw new InvalidOperationException("Customer not found.");

        if (customer.RekazCustomerId is null)
            throw new InvalidOperationException("Customer is not yet synced with Rekaz — cannot create a subscription.");
            // Defensive guard only — should not happen given Rekaz-first is now enforced
            // across every customer-creation pathway (registration, admin-create, bulk import).

        // Find the customer's most relevant existing subscription of the same type.
        var existing = await _context.Subscriptions
            .Where(s => s.CustomerId == customer.Id && s.Type == request.Type)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(ct);

        if (existing is not null && existing.Status == "Pending")
        {
            // Don't create a duplicate pending invoice — return the existing unpaid one.
            return new CreateSubscriptionResult(
                existing.RekazInvoiceId ?? Guid.Empty,
                existing.PaymentLink ?? string.Empty,
                IsRenewalQueued: false,
                EffectiveStartAt: existing.StartDate);
        }

        DateTime startAt;
        var isRenewal = false;
        if (existing is not null && existing.Status == "Active"
            && existing.EndDate.HasValue && existing.EndDate.Value > DateTime.UtcNow)
        {
            // Queue the new subscription to start exactly when the current one ends —
            // avoids overlap/wasted paid days, and avoids needing a separate "update dates"
            // call after creation (Rekaz's create endpoint already accepts startAt directly).
            startAt = existing.EndDate.Value;
            isRenewal = true;
        }
        else
        {
            startAt = DateTime.UtcNow;
        }

        var rekazResult = await _rekazSubscriptionsClient.CreateSubscriptionAsync(new CreateRekazSubscriptionRequest(
            CustomerId: customer.RekazCustomerId,
            NewCustomerDetails: null, // never used — Rekaz-first guarantees RekazCustomerId always exists by now
            StartAt: startAt,
            BranchId: Guid.Parse("3a226f63-0e03-a632-c8c3-acb916184a42"),
            Items: new List<RekazSubscriptionItemInput> { new(request.PriceId, request.Quantity) },
            OccurenceDays: null,
            Discount: null
        ), ct);

        var fullPaymentLink = "https://platform.rekaz.io" + rekazResult.PaymentLink;

        await _unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            // NOTE: Rekaz's create response only returns invoiceId/paymentLink, NOT the new
            // subscription's own id — RekazSubscriptionId stays unset here (Guid.Empty is a
            // placeholder, not a real correlation key). The actual id is only learned when
            // the corresponding SubscriptionCreatedEvent webhook arrives; RekazWebhookProcessor
            // currently matches purely by RekazSubscriptionId, which won't exist on this row
            // yet. TODO (flagged, not silently resolved): design a correlation strategy for
            // matching this pending local row to its eventual webhook (e.g. by CustomerId +
            // StartAt + Pending status, or by RekazInvoiceId if the webhook/re-fetch response
            // ever exposes invoice linkage) — needs a deliberate follow-up decision.

            var subscription = new Subscription(
                id: Guid.NewGuid(),
                customerId: customer.Id,
                rekazSubscriptionId: Guid.Empty, // TODO: see note above
                type: request.Type,
                startDate: startAt,
                price: 0,
                rekazInvoiceId: rekazResult.InvoiceId,
                paymentLink: fullPaymentLink);

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync(innerCt);
            return true;
        }, ct);

        return new CreateSubscriptionResult(rekazResult.InvoiceId, fullPaymentLink, isRenewal, startAt);
    }
}
