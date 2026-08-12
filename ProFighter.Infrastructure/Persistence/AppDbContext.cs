using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProFighter.Domain.Common;
using ProFighter.Domain.Entities;
using ProFighter.Infrastructure.Identity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<MerchandiseOrder> MerchandiseOrders => Set<MerchandiseOrder>();
    public DbSet<Gift> Gifts => Set<Gift>();
    public DbSet<RekazWebhookInboxEntry> RekazWebhookInboxEntries => Set<RekazWebhookInboxEntry>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<Gym> Gyms => Set<Gym>();
    public DbSet<Media> Medias => Set<Media>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must call base.OnModelCreating first when inheriting IdentityDbContext
        base.OnModelCreating(modelBuilder);
        
        // Apply configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplySoftDeleteAndTracking();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplySoftDeleteAndTracking();
        return base.SaveChanges();
    }

    private void ApplySoftDeleteAndTracking()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                    entry.Entity.MarkAsUpdated();
                    break;

                case EntityState.Deleted:
                    // Change state to Modified to prevent hard delete
                    entry.State = EntityState.Modified;
                    entry.Entity.MarkAsDeleted();
                    break;
            }
        }
    }
}
