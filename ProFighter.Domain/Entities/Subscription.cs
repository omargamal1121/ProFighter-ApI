using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public Guid RekazSubscriptionId { get; private set; }
    public Guid? RekazInvoiceId { get; private set; }
    public string? PaymentLink { get; private set; }
    public SubscriptionType Type { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public decimal Price { get; private set; }

    // EF Core Constructor
    private Subscription() : base() { }

    public Subscription(
        Guid id,
        Guid customerId,
        Guid rekazSubscriptionId,
        SubscriptionType type,
        DateTime startDate,
        decimal price,
        Guid? rekazInvoiceId = null,
        string? paymentLink = null) : base()
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(price));

        Id = id;
        CustomerId = customerId;
        RekazSubscriptionId = rekazSubscriptionId;
        Type = type;
        Status = SubscriptionStatus.Pending;
        StartDate = startDate;
        Price = price;
        RekazInvoiceId = rekazInvoiceId;
        PaymentLink = paymentLink;
        CreatedAt = DateTime.UtcNow;
    }

    public void Activate(DateTime? endDate = null)
    {
        Status = SubscriptionStatus.Active;
        if (endDate.HasValue)
        {
            EndDate = endDate.Value;
        }
        MarkAsUpdated();
    }

    public void Pause()
    {
        if (Status != SubscriptionStatus.Active)
            throw new InvalidOperationException("Only active subscriptions can be paused.");
        Status = SubscriptionStatus.Paused;
        MarkAsUpdated();
    }

    public void Resume()
    {
        if (Status != SubscriptionStatus.Paused)
            throw new InvalidOperationException("Only paused subscriptions can be resumed.");
        Status = SubscriptionStatus.Active;
        MarkAsUpdated();
    }

    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
        MarkAsUpdated();
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Cancelled;
        MarkAsUpdated();
    }

    public void TransferToCustomer(Guid newCustomerId)
    {
        CustomerId = newCustomerId;
        Status = SubscriptionStatus.Transferred;
        MarkAsUpdated();
    }

    public void UpdatePaymentDetails(Guid? invoiceId, string? paymentLink)
    {
        RekazInvoiceId = invoiceId;
        PaymentLink = paymentLink;
        MarkAsUpdated();
    }
}
