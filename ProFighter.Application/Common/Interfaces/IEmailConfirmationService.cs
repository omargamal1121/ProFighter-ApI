using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Common.Interfaces;

public interface IEmailConfirmationService
{
    Task SendConfirmationOtpAsync(Guid customerId, ProFighter.Domain.Enums.GymType? gymType = null, CancellationToken ct = default);
}
