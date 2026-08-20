using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable("Subscriptions");

        builder.Property(s => s.PaymentLink)
            .HasMaxLength(2048);

        builder.Property(s => s.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.Price)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(s => s.StartDate)
            .IsRequired();

        // Foreign Key relationships
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(s => s.RekazSubscriptionId)
            .IsUnique();
    }
}
