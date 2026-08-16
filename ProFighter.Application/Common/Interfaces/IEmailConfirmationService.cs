using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Common.Interfaces;

public interface IEmailConfirmationService
{
    Task SendConfirmationOtpAsync(Guid customerId, CancellationToken ct = default);
}
