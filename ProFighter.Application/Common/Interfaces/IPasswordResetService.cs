using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Common.Interfaces;

public interface IPasswordResetService
{
    Task SendPasswordResetOtpAsync(Guid userId, CancellationToken ct = default);
}
