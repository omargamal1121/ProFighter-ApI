using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable(
            "Medias",
            t => t.HasCheckConstraint(
                "CK_Media_SingleOwner",
                // MySQL 8.0.16+ enforces CHECK constraints. Exactly one FK must be non-null.
                "(" +
                "(CASE WHEN `CustomerId` IS NOT NULL THEN 1 ELSE 0 END) + " +
                "(CASE WHEN `TrainerId`  IS NOT NULL THEN 1 ELSE 0 END) + " +
                "(CASE WHEN `GymId`     IS NOT NULL THEN 1 ELSE 0 END) + " +
                "(CASE WHEN `ProductId` IS NOT NULL THEN 1 ELSE 0 END)" +
                ") = 1"));

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

        builder.Property(m => m.Purpose)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.DisplayOrder)
            .IsRequired();

        // Nullable FK columns (exclusive-arc — exactly one non-null per CHECK constraint above)
        builder.Property(m => m.CustomerId).IsRequired(false);
        builder.Property(m => m.TrainerId).IsRequired(false);
        builder.Property(m => m.GymId).IsRequired(false);
        builder.Property(m => m.ProductId).IsRequired(false);

     

        builder.HasOne(m => m.Customer)
            .WithMany(c => c.Media)
            .HasForeignKey(m => m.CustomerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(m => m.Trainer)
            .WithMany(t => t.Medias)
            .HasForeignKey(m => m.TrainerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(m => m.Gym)
            .WithMany(g => g.Media)
            .HasForeignKey(m => m.GymId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(m => m.Product)
            .WithMany(p => p.Media)
            .HasForeignKey(m => m.ProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Indexes ──────────────────────────────────────────────────────────
        //
        // Simple FK indexes (help JOIN / FK-check lookups)
        builder.HasIndex(m => m.CustomerId);
        builder.HasIndex(m => m.TrainerId);
        builder.HasIndex(m => m.GymId);
        builder.HasIndex(m => m.ProductId);

        // Composite indexes — the typical query pattern is "give me all media
        // for owner X with purpose Y ordered by DisplayOrder".
        builder.HasIndex(m => new { m.CustomerId, m.Purpose, m.DisplayOrder });
        builder.HasIndex(m => new { m.TrainerId,  m.Purpose, m.DisplayOrder });
        builder.HasIndex(m => new { m.GymId,      m.Purpose, m.DisplayOrder });
        builder.HasIndex(m => new { m.ProductId,  m.Purpose, m.DisplayOrder });
    }
}
