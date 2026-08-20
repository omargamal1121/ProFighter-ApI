using MediatR;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Queries.GetCustomerSubscriptions;

public record GetCustomerSubscriptionsQuery(
    Guid CustomerId,
    string? Status = null,
    SubscriptionType? Type = null,
    DateTime? StartDateFrom = null,
    DateTime? StartDateTo = null,
    DateTime? EndDateFrom = null,
    DateTime? EndDateTo = null,
    bool? IsActive = null
) : IRequest<GetCustomerSubscriptionsResult>;

public record GetCustomerSubscriptionsResult(List<CustomerSubscriptionDto> Subscriptions, int TotalCount);

public record CustomerSubscriptionDto(
    Guid Id,
    Guid RekazSubscriptionId,
    SubscriptionType Type,
    string Status,
    DateTime StartDate,
    DateTime? EndDate,
    decimal Price,
    bool IsFullyPaid,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
