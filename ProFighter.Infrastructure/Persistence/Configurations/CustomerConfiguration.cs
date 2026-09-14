using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
       
        builder.ConfigureBaseEntity();

        builder.ToTable("Customers");

       
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

        builder.Property(c => c.GymType)
            .HasDefaultValue(Domain.Enums.GymType.ProFighter)
            .IsRequired();

       
        builder.HasIndex(c => new { c.MobileNumber, c.GymType })
            .IsUnique()
            .HasFilter("`DeletedAt` IS NULL");

        
        builder.HasIndex(c => c.RekazCustomerId)
            .IsUnique()
            .HasFilter("`RekazCustomerId` IS NOT NULL AND `DeletedAt` IS NULL");

      
        builder.HasIndex(c => new { c.Email, c.GymType })
            .IsUnique()
            .HasFilter("`Email` IS NOT NULL AND `DeletedAt` IS NULL");
    }
}
