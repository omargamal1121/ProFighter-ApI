using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Apply BaseEntity configuration (Id, ValueGeneratedNever, CreatedAt, soft delete filter)
        builder.ConfigureBaseEntity();

        builder.ToTable("Customers");

        // SHARED PRIMARY KEY PATTERN:
        // The Customer.Id property acts as both the primary key and the foreign key referencing the Identity ApplicationUser.Id.
        // EF Core mapping for this 1:1 relationship is established without introducing a separate ApplicationUserId column.

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.MobileNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(150);

        builder.Property(c => c.Source)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.LoyaltyPointsBalance)
            .IsRequired();

        // Unique filtered index for MobileNumber (soft-delete awareness)
        builder.HasIndex(c => c.MobileNumber)
            .IsUnique()
            .HasFilter("`DeletedAt` IS NULL");

        // Unique filtered indexes for nullable properties and soft-delete awareness
        builder.HasIndex(c => c.RekazCustomerId)
            .IsUnique()
            .HasFilter("`RekazCustomerId` IS NOT NULL AND `DeletedAt` IS NULL");

        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasFilter("`Email` IS NOT NULL AND `DeletedAt` IS NULL");
    }
}
