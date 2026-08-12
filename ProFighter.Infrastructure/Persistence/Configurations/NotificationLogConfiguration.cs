using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable("NotificationLogs");

        builder.Property(n => n.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(n => n.Body)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(n => n.Channel)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Foreign Key
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(n => n.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
