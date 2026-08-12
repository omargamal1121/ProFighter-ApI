using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable("Medias");

        builder.Property(m => m.CloudinaryUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(m => m.CloudinaryPublicId)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(m => m.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.OwnerType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.OwnerId)
            .IsRequired();

        builder.Property(m => m.Purpose)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.DisplayOrder)
            .IsRequired();

        // Indexes
        builder.HasIndex(m => new { m.OwnerType, m.OwnerId });
        builder.HasIndex(m => new { m.OwnerType, m.OwnerId, m.Purpose });
    }
}
