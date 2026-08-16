using System;
using System.Threading.Tasks;
using ProFighter.Application.Common.Models.Auth;

namespace ProFighter.Application.Common.Interfaces.Auth;

public interface IRefreshTokenService
{
    Task<string> GenerateAndStoreAsync(Guid userId, string securityStamp, bool reuseExisting = true);
    Task<RefreshTokenResponse> RotateAsync(string rawToken);
    Task<bool> RevokeAsync(string rawToken);
    Task<bool> RevokeAllForUserAsync(Guid userId);
}
