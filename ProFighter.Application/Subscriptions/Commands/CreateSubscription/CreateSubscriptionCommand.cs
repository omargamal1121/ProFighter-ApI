using MediatR;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Commands.CreateSubscription;

public record CreateSubscriptionCommand(
    Guid CustomerId,      // our local Customer.Id
    SubscriptionType Type,
    Guid PriceId,          // Rekaz priceId for the chosen plan
    int Quantity
) : IRequest<CreateSubscriptionResult>;

public record CreateSubscriptionResult(Guid InvoiceId, string PaymentLink, bool IsRenewalQueued, DateTime EffectiveStartAt);
