namespace ProFighter.Application.Common.Models;

/// <summary>
/// Mirrors the Rekaz PublicProductType enum.
/// 0 = Reservation, 1 = Subscription, 2 = Merchandise
/// </summary>
public enum RekazProductType
{
    Reservation = 0,
    Subscription = 1,
    Merchandise = 2,
}

/// <summary>
/// Query parameters accepted by GET /api/public/products.
/// All properties are optional — omitted values are not sent as query params.
/// </summary>
/// <param name="SkipCount">Zero-based offset. Range: 0–2 147 483 647.</param>
/// <param name="MaxResultCount">Page size. Range: 1–100.</param>
/// <param name="Keyword">Free-text search term.</param>
/// <param name="Type">Filter by product type.</param>
/// <param name="BranchId">Filter by branch.</param>
/// <param name="Sorting">Sort expression accepted by the Rekaz API (e.g. "name asc").</param>
public record RekazProductsQuery(
    int SkipCount = 0,
    int MaxResultCount = 20,
    string? Keyword = null,
    RekazProductType? Type = null,
    Guid? BranchId = null,
    string? Sorting = null
);

/// <summary>
/// Paged result returned to callers requesting Rekaz products.
/// </summary>
public record RekazProductsResult(List<RekazProductSummary> Items, long TotalCount);

/// <summary>
/// A sport/category filter option shown on the products listing screen.
/// These are ProFighter-specific values, not fetched from Rekaz.
/// </summary>
/// <param name="Key">URL-safe slug used as a filter keyword (e.g. "boxing").</param>
/// <param name="Name">Localised display name (Arabic).</param>
public record ProductCategoryDto(string Key, string Name);

/// <summary>
/// Application-level representation of a Rekaz product.
/// Raw Infrastructure DTOs are never exposed beyond the Infrastructure boundary.
/// </summary>
public record RekazProductSummary(
    Guid Id,
    string Name,
    decimal Amount,
    /// <summary>First available image URL (FeaturedImage falling back to first element of Images).</summary>
    string? ImageUrl,
    bool IsOutOfStock,
    int? StockQuantity,
    /// <summary>
    /// Product type derived from the Rekaz "type" field.
    /// 0 = Reservation, 1 = Subscription, 2 = Merchandise.
    /// </summary>
    RekazProductType ProductType,
    /// <summary>Human-readable type label returned by Rekaz (e.g. "Reservation").</summary>
    string? TypeString,
    List<RekazPriceSummary> Prices
);

/// <summary>
/// Application-level representation of a single Rekaz pricing option.
/// </summary>
public record RekazPriceSummary(
    Guid Id,
    string? Name,
    decimal Amount,
    decimal? DiscountedAmount,
    /// <summary>
    /// True when the underlying Rekaz PriceType is 2 (Recurring).
    /// The raw integer type value is intentionally not exposed.
    /// </summary>
    bool IsRecurring
);
