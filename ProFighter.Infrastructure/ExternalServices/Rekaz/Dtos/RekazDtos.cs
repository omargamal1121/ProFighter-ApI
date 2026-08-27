namespace ProFighter.Infrastructure.ExternalServices.Rekaz.Dtos;

/// <summary>
/// Top-level paged response from GET /api/public/products.
/// </summary>
public record RekazProductsResponse(List<RekazProductDto> Items, long TotalCount);

/// <summary>
/// A single product returned by the Rekaz public products endpoint.
/// Only the fields below are mapped; all other Rekaz fields (package,
/// ruleBasedItems, pauseConfig, extraProperties, localizedName, branches,
/// customFields, addOns, etc.) are intentionally ignored.
/// </summary>
public record RekazProductDto(
    Guid Id,
    string Name,
    string? NameAr,
    string? NameEn,
    string? Description,
    string? ShortDescription,
    List<string> Images,
    string? FeaturedImage,
    decimal Amount,
    decimal DiscountedAmount,
    /// <summary>PublicProductType: 0=Reservation, 1=Subscription, 2=Merchandise</summary>
    int Type,
    string? TypeString,
    int? StockQuantity,
    bool IsOutOfStock,
    string? Slug,
    string? Url,
    List<RekazPricingDto> Pricing
);

/// <summary>
/// A pricing option associated with a Rekaz product.
/// </summary>
public record RekazPricingDto(
    Guid Id,
    string? Name,
    /// <summary>PriceType: 1=OneTime, 2=Recurring</summary>
    int Type,
    decimal Amount,
    decimal? DiscountedAmount,
    DateTime? DiscountValidFrom,
    DateTime? DiscountValidUntil,
    string? Sku,
    int? Duration,
    int? BillingCycle,
    RekazStockDto? Stock
);

/// <summary>
/// Stock information for a pricing option.
/// </summary>
public record RekazStockDto(
    int? AvailableQuantity,
    bool IsUnlimited,
    int? RemainingQuantity
);
