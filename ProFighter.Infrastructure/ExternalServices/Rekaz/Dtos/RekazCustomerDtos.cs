namespace ProFighter.Infrastructure.ExternalServices.Rekaz.Dtos;

/// <summary>
/// Response from POST /api/public/customers.
/// Rekaz only returns the new customer's id on creation.
/// </summary>
public record RekazCustomerCreatedDto(Guid CustomerId);

/// <summary>
/// A single customer record returned by GET /api/public/customers
/// and GET /api/public/customers/{id}.
/// </summary>
public record RekazCustomerDto(
    Guid Id,
    string Name,
    int CustomerNumber,
    string MobileNumber,
    string? Email,
    int CustomerType,
    string? Address,
    string? CompanyName,
    Dictionary<string, string>? CustomFields,
    List<Guid>? BranchIds,
    bool IsBlocked
);

/// <summary>
/// Paged list response from GET /api/public/customers.
/// </summary>
public record RekazCustomersListResponse(List<RekazCustomerDto> Items, long TotalCount);
