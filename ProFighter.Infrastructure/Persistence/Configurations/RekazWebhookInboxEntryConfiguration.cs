using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class RekazWebhookInboxEntryConfiguration : IEntityTypeConfiguration<RekazWebhookInboxEntry>
{
    public void Configure(EntityTypeBuilder<RekazWebhookInboxEntry> builder)
    {
        builder.ToTable("RekazWebhookInboxEntries");

        // PK comes from Rekaz, not generated locally
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.EventName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(e => e.RawPayload)
            .HasColumnType("longtext")
            .IsRequired();

        builder.Property(e => e.Processed)
            .IsRequired();

        builder.Property(e => e.ProcessedAt);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Index on (Processed, CreatedAt)
        builder.HasIndex(e => new { e.Processed, e.CreatedAt });
    }
}
