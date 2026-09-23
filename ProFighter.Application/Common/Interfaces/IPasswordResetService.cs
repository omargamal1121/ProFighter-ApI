using System.Threading;
using System.Threading.Tasks;
using ProFighter.Application.Common.Enums;

namespace ProFighter.Application.Common.Interfaces;

public interface IPasswordResetService
{
    Task<PasswordResetOtpResult> SendPasswordResetOtpAsync(Guid userId, CancellationToken ct = default);
}
