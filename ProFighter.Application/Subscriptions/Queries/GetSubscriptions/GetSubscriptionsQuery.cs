using MediatR;
using ProFighter.Domain.Enums;
using ProFighter.Application.Subscriptions.Common;

namespace ProFighter.Application.Subscriptions.Queries.GetSubscriptions;

public record GetSubscriptionsQuery(
    Guid? CustomerId = null,
    string? Status = null,
    SubscriptionType? Type = null,
    DateTime? StartDateFrom = null,
    DateTime? StartDateTo = null,
    DateTime? EndDateFrom = null,
    DateTime? EndDateTo = null,
    bool? IsActive = null,
    int SkipCount = 0,
    int MaxResultCount = 20
) : IRequest<GetSubscriptionsResult>;

public record GetSubscriptionsResult(List<SubscriptionDto> Subscriptions, int TotalCount);
