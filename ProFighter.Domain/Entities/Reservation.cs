using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;
using ProFighter.Domain.ValueObjects;

namespace ProFighter.Domain.Entities;

public class Reservation : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public Guid RekazReservationId { get; private set; }
    public Guid? SubscriptionId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public decimal Price { get; private set; }
    public OrderSnapshot Order { get; private set; }

    // EF Core Constructor
    private Reservation() : base()
    {
        Order = null!;
    }

    public Reservation(
        Guid id,
        Guid customerId,
        Guid rekazReservationId,
        DateTime scheduledAt,
        decimal price,
        OrderSnapshot order,
        Guid? subscriptionId = null) : base()
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(price));

        Id = id;
        CustomerId = customerId;
        RekazReservationId = rekazReservationId;
        ScheduledAt = scheduledAt;
        Price = price;
        Order = order ?? throw new ArgumentNullException(nameof(order));
        SubscriptionId = subscriptionId;
        Status = ReservationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        Status = ReservationStatus.Confirmed;
        MarkAsUpdated();
    }

    public void Complete()
    {
        Status = ReservationStatus.Done;
        MarkAsUpdated();
    }

    public void Cancel()
    {
        Status = ReservationStatus.Cancelled;
        MarkAsUpdated();
    }

    public void ReassignToCustomer(Guid newCustomerId)
    {
        CustomerId = newCustomerId;
        MarkAsUpdated();
    }

    public void UpdateOrder(OrderSnapshot newOrder)
    {
        Order = newOrder ?? throw new ArgumentNullException(nameof(newOrder));
        MarkAsUpdated();
    }
}
