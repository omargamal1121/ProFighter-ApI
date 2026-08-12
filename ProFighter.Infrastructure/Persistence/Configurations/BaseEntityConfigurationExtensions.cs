using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Common;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public static class BaseEntityConfigurationExtensions
{
    public static void ConfigureBaseEntity<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.DeletedAt);

        // Soft delete global query filter
        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
