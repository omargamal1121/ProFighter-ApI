namespace ProFighter.Application.Common.Models;

/// <summary>
/// Input for creating a new customer via the Rekaz API.
/// The "type" field is fixed to 12 (the only allowed value per Rekaz docs)
/// and is set internally by the Infrastructure implementation — not exposed here.
/// </summary>
/// <param name="Name">Full name of the customer. Required.</param>
/// <param name="MobileNumber">Mobile phone number. Required.</param>
/// <param name="Email">Optional email address.</param>
/// <param name="Address">Optional address.</param>
/// <param name="VatNumber">Optional VAT / tax number.</param>
/// <param name="BranchId">Optional branch to associate the customer with.</param>
/// <param name="CompanyName">Optional company name for B2B customers.</param>
/// <param name="CustomFields">Optional arbitrary key-value pairs supported by the Rekaz tenant.</param>
/// <param name="BirthDate">Optional birth date.</param>
public record CreateRekazCustomerRequest(
    string Name,
    string MobileNumber,
    string? Email = null,
    string? Address = null,
    string? VatNumber = null,
    Guid? BranchId = null,
    string? CompanyName = null,
    Dictionary<string, object>? CustomFields = null,
    DateOnly? BirthDate = null
);

/// <summary>
/// Query parameters for GET /api/public/customers.
/// </summary>
/// <param name="SkipCount">Zero-based offset. Range: 0–2 147 483 647.</param>
/// <param name="MaxResultCount">Page size. Range: 1–100 (clamped in implementation).</param>
/// <param name="MobileNumber">Optional mobile number filter.</param>
/// <param name="Sorting">Optional sort expression (e.g. "name asc").</param>
public record RekazCustomersQuery(
    int SkipCount = 0,
    int MaxResultCount = 20,
    string? MobileNumber = null,
    string? Sorting = null
);

/// <summary>
/// Application-level representation of a single Rekaz customer.
/// Raw Infrastructure DTOs (RekazCustomerDto) are never exposed beyond the Infrastructure boundary.
/// </summary>
public record RekazCustomerResult(
    Guid Id,
    string Name,
    int CustomerNumber,
    string MobileNumber,
    string? Email,
    string? Address,
    string? CompanyName,
    bool IsBlocked
);

/// <summary>
/// Paged result returned to callers requesting a list of Rekaz customers.
/// </summary>
public record RekazCustomersListResult(List<RekazCustomerResult> Items, long TotalCount);
