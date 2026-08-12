using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class MerchandiseOrderConfiguration : IEntityTypeConfiguration<MerchandiseOrder>
{
    public void Configure(EntityTypeBuilder<MerchandiseOrder> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable("MerchandiseOrders");

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasPrecision(10, 2)
            .IsRequired();

        // Foreign Key
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Owned items collection
        builder.OwnsMany(o => o.Items, item =>
        {
            item.ToTable("MerchandiseOrderItems");
            
            // Define composite key for Owned Type
            item.WithOwner().HasForeignKey("MerchandiseOrderId");
            item.HasKey("MerchandiseOrderId", "ProductId");

            item.Property(i => i.Quantity)
                .IsRequired();

            item.Property(i => i.UnitPrice)
                .HasPrecision(10, 2)
                .IsRequired();

            // Set up relationship to Product if desired, but not strictly required
            item.HasOne<Product>()
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Navigation configuration to use backing field _items
        builder.Metadata.FindNavigation(nameof(MerchandiseOrder.Items))?
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
