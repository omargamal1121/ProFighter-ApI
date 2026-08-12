using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class MerchandiseOrder : BaseEntity
{
    private readonly List<MerchandiseOrderItem> _items = new();

    public Guid CustomerId { get; private set; }
    public Guid RekazOrderId { get; private set; }
    public MerchandiseOrderStatus Status { get; private set; }
    public IReadOnlyCollection<MerchandiseOrderItem> Items => _items.AsReadOnly();
    public decimal TotalAmount { get; private set; }

    // EF Core Constructor
    private MerchandiseOrder() : base() { }

    public MerchandiseOrder(Guid id, Guid customerId, Guid rekazOrderId) : base()
    {
        Id = id;
        CustomerId = customerId;
        RekazOrderId = rekazOrderId;
        Status = MerchandiseOrderStatus.Created;
        TotalAmount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (Status != MerchandiseOrderStatus.Created)
            throw new InvalidOperationException("Cannot add items to an order that is not in Created status.");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            _items.Remove(existingItem);
            quantity += existingItem.Quantity;
        }

        _items.Add(new MerchandiseOrderItem(productId, quantity, unitPrice));
        RecalculateTotal();
        MarkAsUpdated();
    }

    public void Complete()
    {
        if (Status != MerchandiseOrderStatus.Created)
            throw new InvalidOperationException("Only created orders can be completed.");
        Status = MerchandiseOrderStatus.Completed;
        MarkAsUpdated();
    }

    public void Cancel()
    {
        if (Status == MerchandiseOrderStatus.Completed)
            throw new InvalidOperationException("Completed orders cannot be canceled.");
        Status = MerchandiseOrderStatus.Canceled;
        MarkAsUpdated();
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(item => item.Quantity * item.UnitPrice);
    }
}
