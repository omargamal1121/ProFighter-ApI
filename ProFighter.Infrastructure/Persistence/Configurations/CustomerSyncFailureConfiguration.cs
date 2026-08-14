using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class CustomerSyncFailureConfiguration : IEntityTypeConfiguration<CustomerSyncFailure>
{
    public void Configure(EntityTypeBuilder<CustomerSyncFailure> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable("CustomerSyncFailures");

        builder.Property(c => c.PayloadJson)
            .IsRequired();

        builder.Property(c => c.ErrorMessage)
            .IsRequired();

        builder.Property(c => c.Status)
            .IsRequired()
            .HasMaxLength(50);
    }
}
