using System.Collections.Generic;

namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

public class RekazSubscriptionTypeOptions
{
    public const string SectionName = "RekazSubscriptionTypeMapping";

    /// <summary>
    /// Product ID to SubscriptionType name dictionary for ProFighter.
    /// Example: { "00000000-0000-0000-0000-000000000001": "Swimming" }
    /// </summary>
    public Dictionary<string, string> ProFighter { get; set; } = new();

    /// <summary>
    /// Product ID to SubscriptionType name dictionary for ProGym.
    /// Example: { "00000000-0000-0000-0000-000000000002": "Swimming" }
    /// </summary>
    public Dictionary<string, string> ProGym { get; set; } = new();
}
