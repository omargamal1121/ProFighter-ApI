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

        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired(false);

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

        // Composite indexes for GetMySubscriptions query patterns.
        // All queries filter by (CustomerId, Status) so this is the base covering index.
        builder.HasIndex(s => new { s.CustomerId, s.Status })
            .HasDatabaseName("IX_Subscriptions_CustomerId_Status");

        // Active group: ORDER BY EndDate ASC
        builder.HasIndex(s => new { s.CustomerId, s.Status, s.EndDate })
            .HasDatabaseName("IX_Subscriptions_CustomerId_Status_EndDate");

        // StartingSoon group: ORDER BY StartDate ASC
        builder.HasIndex(s => new { s.CustomerId, s.Status, s.StartDate })
            .HasDatabaseName("IX_Subscriptions_CustomerId_Status_StartDate");

        // Pending / Paused groups: ORDER BY CreatedAt DESC
        builder.HasIndex(s => new { s.CustomerId, s.Status, s.CreatedAt })
            .HasDatabaseName("IX_Subscriptions_CustomerId_Status_CreatedAt");
    }
}
