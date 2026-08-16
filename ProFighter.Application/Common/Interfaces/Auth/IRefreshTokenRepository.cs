using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProFighter.Domain.Entities;

namespace ProFighter.Application.Common.Interfaces.Auth;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    void Update(RefreshToken token);
    Task<RefreshToken?> FindByHashAsync(string tokenHash);
    Task<RefreshToken?> GetReusableAsync(Guid userId, string securityStamp);
    Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId);
}
