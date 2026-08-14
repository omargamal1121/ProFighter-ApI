using ProFighter.Application.Common.Models;

namespace ProFighter.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the Rekaz public customers resource.
/// Implementations live in Infrastructure.
/// </summary>
public interface IRekazCustomersClient
{
    /// <summary>
    /// Creates a new customer in Rekaz.
    /// </summary>
    /// <param name="request">Customer data to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The ID of the newly created customer.</returns>
    Task<Guid> CreateCustomerAsync(CreateRekazCustomerRequest request, CancellationToken ct = default);

    /// <summary>
    /// Fetches a paged list of customers from Rekaz.
    /// </summary>
    Task<RekazCustomersListResult> GetCustomersAsync(RekazCustomersQuery query, CancellationToken ct = default);

    /// <summary>
    /// Fetches a single customer by ID. Returns null if not found (404).
    /// </summary>
    Task<RekazCustomerResult?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default);
}
