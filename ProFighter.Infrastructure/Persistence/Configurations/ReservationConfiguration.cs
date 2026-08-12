using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable("Reservations");

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Price)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(r => r.ScheduledAt)
            .IsRequired();

        // OwnsOne for OrderSnapshot Value Object
        builder.OwnsOne(r => r.Order, order =>
        {
            order.Property(o => o.Subtotal)
                .HasColumnName("OrderSubtotal")
                .HasPrecision(10, 2)
                .IsRequired();

            order.Property(o => o.DiscountAmount)
                .HasColumnName("OrderDiscountAmount")
                .HasPrecision(10, 2)
                .IsRequired();

            order.Property(o => o.TaxAmount)
                .HasColumnName("OrderTaxAmount")
                .HasPrecision(10, 2)
                .IsRequired();

            order.Property(o => o.TotalAmount)
                .HasColumnName("OrderTotalAmount")
                .HasPrecision(10, 2)
                .IsRequired();

            order.Property(o => o.PaidAmount)
                .HasColumnName("OrderPaidAmount")
                .HasPrecision(10, 2)
                .IsRequired();

            order.Property(o => o.RemainingAmount)
                .HasColumnName("OrderRemainingAmount")
                .HasPrecision(10, 2)
                .IsRequired();

            order.Property(o => o.Currency)
                .HasColumnName("OrderCurrency")
                .HasMaxLength(3)
                .IsRequired();

            order.Property(o => o.OrderStatus)
                .HasColumnName("OrderStatus")
                .HasMaxLength(50)
                .IsRequired();

            order.Property(o => o.OrderPaymentStatus)
                .HasColumnName("OrderPaymentStatus")
                .HasMaxLength(50)
                .IsRequired();
        });

        // Foreign Keys
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(r => r.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull); // Nullable FK gets SetNull

        // Indexes
        builder.HasIndex(r => r.RekazReservationId)
            .IsUnique();
    }
}
