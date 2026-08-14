using ProFighter.Domain.Enums;

namespace ProFighter.Application.Common.Interfaces;

public interface ICustomerProvisioningService
{
    Task<Guid> ProvisionLocalCustomerAsync(
        Guid rekazCustomerId,
        string name,
        string mobileNumber,
        string? email,
        CustomerSource source,
        CancellationToken ct = default);
}
