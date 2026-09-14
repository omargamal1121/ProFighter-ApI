namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

/// <summary>
/// API credentials for a single gym's Rekaz account.
/// Values are read from environment variables / user-secrets — never hardcoded.
///
/// Configuration path example:
///   Rekaz:ProFighter:ApiKeyBase64
///   Rekaz:ProFighter:TenantId
///   Rekaz:ProGym:ApiKeyBase64
///   Rekaz:ProGym:TenantId
/// </summary>
public sealed class RekazGymCredentials
{
    /// <summary>
    /// Base64-encoded API key used as the HTTP Basic auth credential for this gym.
    /// Map to environment variable: Rekaz__ProFighter__ApiKeyBase64 (or Rekaz__ProGym__ApiKeyBase64).
    /// </summary>
    public string ApiKeyBase64 { get; set; } = null!;

    /// <summary>
    /// Rekaz tenant ID (the __tenant header) specific to this gym.
    /// Map to environment variable: Rekaz__ProFighter__TenantId (or Rekaz__ProGym__TenantId).
    /// </summary>
    public string TenantId { get; set; } = null!;

    /// <summary>
    /// Rekaz branch ID used when creating subscriptions for this gym.
    /// Map to environment variable: Rekaz__ProFighter__BranchId (or Rekaz__ProGym__BranchId).
    /// </summary>
    public Guid? BranchId { get; set; }
}

/// <summary>
/// Options object bound to the "Rekaz" configuration section, containing
/// a named set of credentials per gym type in addition to the shared BaseUrl.
/// </summary>
public sealed class RekazMultiGymOptions
{
    /// <summary>Rekaz base URL, shared across all gyms.</summary>
    public string BaseUrl { get; set; } = "https://platform.rekaz.io";

    /// <summary>Credentials for the Pro Fighter gym Rekaz account.</summary>
    public RekazGymCredentials ProFighter { get; set; } = null!;

    /// <summary>Credentials for the Pro Gym Rekaz account.</summary>
    public RekazGymCredentials ProGym { get; set; } = null!;
}
