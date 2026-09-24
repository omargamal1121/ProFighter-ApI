using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class Product : BaseEntity
{
    private readonly List<Media> _media = new();

    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public GymType GymType { get; private set; } = GymType.ProFighter;

    public IReadOnlyCollection<Media> Media => _media.AsReadOnly();

    // EF Core Constructor
    private Product() : base()
    {
        Name = null!;
    }

    public Product(Guid id, string name, decimal price, int stockQuantity, GymType gymType = GymType.ProFighter) : base()
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(price));
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));

        Id = id;
        Name = name;
        Price = price;
        StockQuantity = stockQuantity;
        GymType = gymType;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(newPrice));
        Price = newPrice;
        MarkAsUpdated();
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(quantity));
        StockQuantity = quantity;
        MarkAsUpdated();
    }

    public void Restock(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Restock amount must be greater than zero.", nameof(amount));
        StockQuantity += amount;
        MarkAsUpdated();
    }

    public void Sell(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Sold amount must be greater than zero.", nameof(amount));
        if (StockQuantity < amount)
            throw new InvalidOperationException("Not enough stock available.");
        StockQuantity -= amount;
        MarkAsUpdated();
    }
}
