namespace ProFighter.Infrastructure.ExternalServices.Rekaz.Dtos;

/// <summary>
/// A single subscription record returned by GET /api/public/subscriptions
/// and GET /api/public/subscriptions/{id}.
/// </summary>
public record RekazSubscriptionDto(
    Guid Id,
    string SubscriptionCode,
    Guid CustomerId,
    DateTime StartAt,
    DateTime? EndAt,
    int Status,
    decimal PaidAmount,
    decimal TotalAmount,
    decimal RemainingAmount,
    int LastInvoiceStatus,
    bool IsPaused,
    DateTime? PausedAt,
    DateTime? ResumeAt,
    Guid? BranchId,
    DateTime CreationTime,
    DateTime? LastModificationTime,
    List<RekazSubscriptionItemDto> Items,
    RekazDiscountDto? Discount
);

/// <summary>
/// An item inside a subscription DTO.
/// </summary>
public record RekazSubscriptionItemDto(
    Guid Id,
    Guid PriceId,
    string? Name,
    string? ProductName,
    int Quantity
);

/// <summary>
/// Discount information nested inside a subscription DTO.
/// </summary>
public record RekazDiscountDto(int Type, decimal Value);

/// <summary>
/// Paged response list from GET /api/public/subscriptions.
/// </summary>
public record RekazSubscriptionsListResponse(List<RekazSubscriptionDto> Items, long TotalCount);

/// <summary>
/// POST response returned from creating a subscription.
/// </summary>
public record RekazSubscriptionCreatedDto(Guid InvoiceId, string PaymentLink);
