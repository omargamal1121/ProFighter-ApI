using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public Guid RekazSubscriptionId { get; private set; }
    public Guid? RekazInvoiceId { get; private set; }
    public string? PaymentLink { get; private set; }
    public string? Name { get; private set; }
    public SubscriptionType Type { get; private set; }
    public string Status { get; private set; }
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
        string? paymentLink = null,
        string? name = null) : base()
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(price));

        Id = id;
        CustomerId = customerId;
        RekazSubscriptionId = rekazSubscriptionId;
        Type = type;
        Status = "Pending";
        StartDate = startDate;
        Price = price;
        RekazInvoiceId = rekazInvoiceId;
        PaymentLink = paymentLink;
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public void Activate(DateTime? endDate = null)
    {
        Status = "Active";
        if (endDate.HasValue)
        {
            EndDate = endDate.Value;
        }
        MarkAsUpdated();
    }

    public void Pause()
    {
        if (Status != "Active")
            throw new InvalidOperationException("Only active subscriptions can be paused.");
        Status = "Paused";
        MarkAsUpdated();
    }

    public void Resume()
    {
        if (Status != "Paused")
            throw new InvalidOperationException("Only paused subscriptions can be resumed.");
        Status = "Active";
        MarkAsUpdated();
    }

    public void Expire()
    {
        Status = "Expired";
        MarkAsUpdated();
    }

    public void Cancel()
    {
        Status = "Cancelled";
        MarkAsUpdated();
    }

    public void TransferToCustomer(Guid newCustomerId)
    {
        CustomerId = newCustomerId;
        Status = "Transferred";
        MarkAsUpdated();
    }

    public void UpdatePaymentDetails(Guid? invoiceId, string? paymentLink)
    {
        RekazInvoiceId = invoiceId;
        PaymentLink = paymentLink;
        MarkAsUpdated();
    }

    public void SyncFromRekaz(string status, DateTime startDate, DateTime? endDate, decimal price, string? name = null)
    {
        Status = status;
        StartDate = startDate;
        EndDate = endDate;
        Price = price;
        if (name != null)
        {
            Name = name;
        }
        MarkAsUpdated();
    }

    public void SetName(string? name)
    {
        Name = name;
        MarkAsUpdated();
    }

    public void SetRekazInvoice(Guid invoiceId, string paymentLink)
    {
        RekazInvoiceId = invoiceId;
        PaymentLink = paymentLink;
        MarkAsUpdated();
    }
}
