using ProFighter.Application.Common.Models;

namespace ProFighter.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the Rekaz public subscriptions resource.
/// Implementations live in Infrastructure.
/// </summary>
public interface IRekazSubscriptionsClient
{
    /// <summary>
    /// Creates a subscription in Rekaz.
    /// </summary>
    /// <param name="request">Subscription parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<RekazSubscriptionCreatedResult> CreateSubscriptionAsync(CreateRekazSubscriptionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a paged list of subscriptions from Rekaz.
    /// </summary>
    Task<RekazSubscriptionsListResult> GetSubscriptionsAsync(RekazSubscriptionsQuery query, CancellationToken ct = default);

    /// <summary>
    /// Gets a single subscription by ID from Rekaz. Returns null if response status is 404.
    /// </summary>
    Task<RekazSubscriptionResult?> GetSubscriptionByIdAsync(Guid id, CancellationToken ct = default);
}
