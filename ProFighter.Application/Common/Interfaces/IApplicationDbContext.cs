using Microsoft.EntityFrameworkCore;
using ProFighter.Domain.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ProFighter.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<CustomerSyncFailure> CustomerSyncFailures { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<RekazWebhookInboxEntry> RekazWebhookInboxEntries { get; }
    DbSet<DeviceToken> DeviceTokens { get; }
    DbSet<Trainer> Trainers { get; }
    DbSet<Media> Medias { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
