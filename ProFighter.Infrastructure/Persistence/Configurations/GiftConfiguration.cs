using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class GiftConfiguration : IEntityTypeConfiguration<Gift>
{
    public void Configure(EntityTypeBuilder<Gift> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable("Gifts");

        builder.Property(g => g.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(g => g.Value)
            .HasPrecision(10, 2)
            .IsRequired();

        // Foreign Key
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(g => g.RecipientCustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index on RekazGiftId
        builder.HasIndex(g => g.RekazGiftId)
            .IsUnique();
    }
}
