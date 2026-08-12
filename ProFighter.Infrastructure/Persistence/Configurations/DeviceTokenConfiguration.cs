using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable("DeviceTokens");

        builder.Property(d => d.FcmToken)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.LastUsedAt)
            .IsRequired();

        // Foreign Key
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index
        builder.HasIndex(d => d.FcmToken)
            .IsUnique();
    }
}
