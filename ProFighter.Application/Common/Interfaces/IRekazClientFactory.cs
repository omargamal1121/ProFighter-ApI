using ProFighter.Domain.Enums;

namespace ProFighter.Application.Common.Interfaces;

/// <summary>
/// Resolves a gym-specific Rekaz HTTP client bundle (customers + subscriptions),
/// pre-configured with the correct API key and tenant ID for the requested gym.
/// </summary>
public interface IRekazClientFactory
{
    /// <summary>
    /// Returns a pre-configured <see cref="IRekazClient"/> for the given gym.
    /// Throws <see cref="InvalidOperationException"/> if no API key is configured
    /// for the requested <paramref name="gymType"/>.
    /// </summary>
    IRekazClient GetClient(GymType gymType);
}
