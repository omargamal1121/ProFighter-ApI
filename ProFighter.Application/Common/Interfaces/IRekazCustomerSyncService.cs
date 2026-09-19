using System;
using System.Threading;
using System.Threading.Tasks;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Common.Interfaces;

public interface IRekazCustomerSyncService
{
    Task<Customer> EnsureLocalCustomerAsync(Guid rekazCustomerId, CancellationToken ct);
    Task<Customer> EnsureLocalCustomerAsync(Guid rekazCustomerId, GymType gymType, CancellationToken ct = default);
}
