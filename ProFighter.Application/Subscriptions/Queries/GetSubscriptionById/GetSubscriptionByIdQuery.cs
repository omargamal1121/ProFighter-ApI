using MediatR;
using ProFighter.Application.Subscriptions.Common;

namespace ProFighter.Application.Subscriptions.Queries.GetSubscriptionById;

public record GetSubscriptionByIdQuery(Guid RekazSubscriptionId) : IRequest<GetSubscriptionByIdResult>;

public record GetSubscriptionByIdResult(SubscriptionDto? Subscription);
