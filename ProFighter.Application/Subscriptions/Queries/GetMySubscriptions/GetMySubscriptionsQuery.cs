using MediatR;

namespace ProFighter.Application.Subscriptions.Queries.GetMySubscriptions;

/// <summary>
/// Returns the authenticated user's subscriptions from the local database.
/// CustomerId is resolved server-side from the JWT — never supplied by the client.
/// </summary>
public record GetMySubscriptionsQuery(
    /// <summary>Resolved from JWT (Customer.Id == IdentityUser.Id).</summary>
    Guid CustomerId,

    /// <summary>Optional status filter (e.g. "Active", "Pending"). Null = return all statuses.</summary>
    string? Status = null,

    int Page = 1,
    int PageSize = 20
) : IRequest<GetMySubscriptionsResult>;

public record GetMySubscriptionsResult(
    List<MySubscriptionDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record MySubscriptionDto(
    Guid Id,
    Guid RekazSubscriptionId,
    string? Name,
    string Type,
    string Status,
    DateTime StartDate,
    DateTime? EndDate,
    decimal Price,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? PaymentLink
);
