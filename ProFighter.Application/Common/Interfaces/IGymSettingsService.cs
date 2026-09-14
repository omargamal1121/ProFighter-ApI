using ProFighter.Domain.Enums;

namespace ProFighter.Application.Common.Interfaces;

/// <summary>
/// Provides gym-specific configuration values (e.g. Rekaz branch ID).
/// Implemented in Infrastructure; configured via environment variables or user-secrets.
/// Config path: Rekaz:{GymTypeName}:BranchId
/// </summary>
public interface IGymSettingsService
{
    /// <summary>
    /// Returns the Rekaz branch ID configured for the given gym, or null if not set.
    /// </summary>
    Guid? GetBranchId(GymType gymType);
}
