using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Configurations;

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.ConfigureBaseEntity();

        builder.ToTable("LoyaltyTransactions");

        builder.Property(lt => lt.Points)
            .IsRequired();

        builder.Property(lt => lt.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(lt => lt.SourceReference)
            .HasMaxLength(250);

        // Foreign Key
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(lt => lt.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(lt => lt.CustomerId);
        builder.HasIndex(lt => lt.SourceReference);
    }
}
