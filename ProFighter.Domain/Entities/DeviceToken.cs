using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class DeviceToken : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string FcmToken { get; private set; }
    public DateTime LastUsedAt { get; private set; }
    public GymType GymType { get; private set; } = GymType.ProFighter;

    // EF Core Constructor
    private DeviceToken() : base()
    {
        FcmToken = null!;
    }

    public Guid ReassignToCustomer(Guid newCustomerId)
    {
        CustomerId = newCustomerId;
        LastUsedAt = DateTime.UtcNow;
        MarkAsUpdated();
        return CustomerId;
    }

    public DeviceToken(Guid id, Guid customerId, string fcmToken, GymType gymType = GymType.ProFighter) : base()
    {
        if (string.IsNullOrWhiteSpace(fcmToken))
            throw new ArgumentException("FCM token cannot be empty.", nameof(fcmToken));

        Id = id;
        CustomerId = customerId;
        FcmToken = fcmToken;
        LastUsedAt = DateTime.UtcNow;
        GymType = gymType;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}
