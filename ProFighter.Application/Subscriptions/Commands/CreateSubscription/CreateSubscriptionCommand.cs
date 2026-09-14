using MediatR;

namespace ProFighter.Application.Subscriptions.Commands.CreateSubscription;

public record CreateSubscriptionCommand(
    Guid CustomerId,
    string PlanName,   // Plan name used to detect renewal (replaces Type filter)
    Guid PriceId,
    int Quantity
) : IRequest<CreateSubscriptionResult>;

public record CreateSubscriptionResult(Guid InvoiceId, string PaymentLink, bool IsRenewalQueued, DateTime EffectiveStartAt);
