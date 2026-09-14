using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class LoyaltyTransaction : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public int Points { get; private set; }
    public LoyaltyTransactionType Type { get; private set; }
    public string? SourceReference { get; private set; }
    public GymType GymType { get; private set; } = GymType.ProFighter;

    // EF Core Constructor
    private LoyaltyTransaction() : base() { }

    public LoyaltyTransaction(
        Guid id,
        Guid customerId,
        int points,
        LoyaltyTransactionType type,
        string? sourceReference = null,
        GymType gymType = GymType.ProFighter) : base()
    {
        Id = id;
        CustomerId = customerId;
        Points = points;
        Type = type;
        SourceReference = sourceReference;
        GymType = gymType;
        CreatedAt = DateTime.UtcNow;
    }
}
