using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Common.Interfaces;

public interface IAuthenticationService
{
    Task<CredentialCheckResult> ValidateCredentialsAsync(string mobileNumber, string password, CancellationToken ct = default);
    Task SetPasswordAndEmailAsync(Guid userId, string newPassword, string email, CancellationToken ct = default);
    Task<IList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsEmailConfirmedAsync(Guid userId, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);
    Task ConfirmEmailAsync(Guid userId, CancellationToken ct = default);
}

public record CredentialCheckResult(bool Succeeded, Guid? UserId, bool IsFirstLogin, IList<string> Roles);
