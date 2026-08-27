using System;
using System.Threading;
using System.Threading.Tasks;
using ProFighter.Domain.Entities;

namespace ProFighter.Application.Common.Interfaces;

public interface IRekazCustomerSyncService
{
    Task<Customer> EnsureLocalCustomerAsync(Guid rekazCustomerId, CancellationToken ct);
}
