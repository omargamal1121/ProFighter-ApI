using Microsoft.EntityFrameworkCore;
using ProFighter.Domain.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ProFighter.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<CustomerSyncFailure> CustomerSyncFailures { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
