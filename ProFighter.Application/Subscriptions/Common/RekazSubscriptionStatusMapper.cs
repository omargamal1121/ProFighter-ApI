// This mapper is no longer needed since we store raw Rekaz status strings directly.
// File kept for reference but should be removed once verified.

namespace ProFighter.Application.Subscriptions.Common;

public static class RekazSubscriptionStatusMapper
{
    // No longer used - we store raw Rekaz status strings directly in the database
    public static string Map(string rekazStatus) => rekazStatus;
}
