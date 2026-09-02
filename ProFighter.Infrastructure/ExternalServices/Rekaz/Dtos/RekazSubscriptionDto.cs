using System.Text.Json.Serialization;

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
    string Status,
    decimal PaidAmount,
    decimal TotalAmount,
    decimal RemainingAmount,
    string LastInvoiceStatus,
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
/// Localized name object returned by Rekaz API inside an item (e.g. localizedProductName).
/// Contains language codes as keys (e.g. "ar", "en") and the translated names as values.
/// </summary>
public record RekazLocalizedNameDto(
    Dictionary<string, string>? OtherLanguages
);

/// <summary>
/// An item inside a subscription DTO.
/// Additional fields from Rekaz API are ignored using JsonExtensionData to prevent deserialization errors.
/// </summary>
public record RekazSubscriptionItemDto(
    Guid Id,
    Guid PriceId,
    string? Name,
    string? ProductName,
    int Quantity,
    RekazLocalizedNameDto? LocalizedProductName = null
);

/// <summary>
/// Discount information nested inside a subscription DTO.
/// Type is string to handle both numeric ("0") and string ("0") representations from Rekaz API.
/// Uses custom converter to normalize both formats.
/// </summary>
public record RekazDiscountDto(string Type, decimal Value);

/// <summary>
/// Paged response list from GET /api/public/subscriptions.
/// </summary>
public record RekazSubscriptionsListResponse(List<RekazSubscriptionDto> Items, long TotalCount);

/// <summary>
/// POST response returned from creating a subscription.
/// </summary>
public record RekazSubscriptionCreatedDto(Guid InvoiceId, string PaymentLink);
