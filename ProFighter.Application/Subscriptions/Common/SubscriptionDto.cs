namespace ProFighter.Application.Subscriptions.Common;

public record SubscriptionDto(
    Guid Id,
    Guid CustomerId,
    Guid RekazSubscriptionId,
    string Type,
    string Status,
    DateTime StartDate,
    DateTime? EndDate,
    decimal Price,
    bool IsFullyPaid,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
