using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class Gift : BaseEntity
{
    public Guid RekazGiftId { get; private set; }
    public Guid? RecipientCustomerId { get; private set; }
    public GiftStatus Status { get; private set; }
    public decimal Value { get; private set; }

    // EF Core Constructor
    private Gift() : base() { }

    public Gift(Guid id, Guid rekazGiftId, decimal value, Guid? recipientCustomerId = null) : base()
    {
        if (value < 0)
            throw new ArgumentException("Value cannot be negative.", nameof(value));

        Id = id;
        RekazGiftId = rekazGiftId;
        Value = value;
        RecipientCustomerId = recipientCustomerId;
        Status = GiftStatus.Created;
        CreatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = GiftStatus.Activated;
        MarkAsUpdated();
    }

    public void Redeem(Guid recipientCustomerId)
    {
        if (Status != GiftStatus.Activated && Status != GiftStatus.Created)
            throw new InvalidOperationException("Gift must be in Created or Activated status to redeem.");

        RecipientCustomerId = recipientCustomerId;
        Status = GiftStatus.Redeemed;
        MarkAsUpdated();
    }

    public void Cancel()
    {
        if (Status == GiftStatus.Redeemed)
            throw new InvalidOperationException("Redeemed gifts cannot be cancelled.");
        Status = GiftStatus.Cancelled;
        MarkAsUpdated();
    }

    public void AssignRecipient(Guid recipientCustomerId)
    {
        RecipientCustomerId = recipientCustomerId;
        MarkAsUpdated();
    }
}
