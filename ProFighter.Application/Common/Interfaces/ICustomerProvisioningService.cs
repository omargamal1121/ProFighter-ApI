using System;
using System.Threading;
using System.Threading.Tasks;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Common.Interfaces;

public interface ICustomerProvisioningService
{
    Task<Guid> ProvisionFromRekazAsync(
        Guid rekazCustomerId,
        string name,
        string mobileNumber,
        string? email,
        CancellationToken ct = default);

    Task<Guid> ProvisionLocalCustomerWithPasswordAsync(
        Guid rekazCustomerId, string name, string mobileNumber, string? email,
        string password, CustomerSource source, CancellationToken ct = default);

    Task<Customer> ProvisionLocalCustomerAsync(
        Guid rekazCustomerId, string name, string mobileNumber, string? email,
        CustomerSource source, CancellationToken ct = default);
}

