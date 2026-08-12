using ProFighter.Application.Common.Models;

namespace ProFighter.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the Rekaz public products resource.
/// Implementations live in Infrastructure.
/// </summary>
public interface IRekazProductsClient
{
    /// <summary>
    /// Fetches a paged, filtered list of Rekaz products.
    /// </summary>
    /// <param name="query">Filter and pagination parameters. All fields are optional.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<RekazProductsResult> GetProductsAsync(
        RekazProductsQuery query,
        CancellationToken ct = default);
}

