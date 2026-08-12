using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class TrainerConfiguration : IEntityTypeConfiguration<Trainer>
{
    public void Configure(EntityTypeBuilder<Trainer> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable("Trainers");

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.Bio)
            .HasMaxLength(1000);

        builder.Property(t => t.Specialization)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .IsRequired();

        // Polymorphic relationship to Media (No database-level FK constraint enforced)
        builder.HasMany(t => t.Medias)
            .WithOne()
            .HasForeignKey(m => m.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
