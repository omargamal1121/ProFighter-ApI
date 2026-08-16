using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Common;

public static class RekazSubscriptionStatusMapper
{
    // Rekaz: 1=Pending, 2=Active, 3=Cancelled, 4=Suspended, 5=Expired, 6=Paused, 7=Transferred, 8=StartingSoon
    // Local SubscriptionStatus: Pending, Active, Paused, Expired, Cancelled, Transferred
    // Suspended and StartingSoon have no exact local equivalent — mapped to the closest
    // practical meaning for this project. TODO: revisit if Suspended ever needs distinct
    // handling from Paused.
    public static SubscriptionStatus Map(int rekazStatus) => rekazStatus switch
    {
        1 => SubscriptionStatus.Pending,
        2 => SubscriptionStatus.Active,
        3 => SubscriptionStatus.Cancelled,
        4 => SubscriptionStatus.Paused,      // Suspended → Paused
        5 => SubscriptionStatus.Expired,
        6 => SubscriptionStatus.Paused,
        7 => SubscriptionStatus.Transferred,
        8 => SubscriptionStatus.Pending,     // StartingSoon → Pending (not yet in effect)
        _ => throw new ArgumentOutOfRangeException(nameof(rekazStatus), $"Unknown Rekaz subscription status: {rekazStatus}")
    };
}
