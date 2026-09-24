using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class GymConfiguration : IEntityTypeConfiguration<Gym>
{
    public void Configure(EntityTypeBuilder<Gym> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable("Gyms");

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(g => g.Description)
            .HasMaxLength(2000);

        builder.Property(g => g.Address)
            .HasMaxLength(300);

        builder.Property(g => g.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(g => g.Email)
            .HasMaxLength(150);
    }
}
