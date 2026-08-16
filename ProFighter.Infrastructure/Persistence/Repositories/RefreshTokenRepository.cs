using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Interfaces.Auth;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IApplicationDbContext _context;

    public RefreshTokenRepository(IApplicationDbContext context) => _context = context;

    public async Task AddAsync(RefreshToken token) =>
        await _context.RefreshTokens.AddAsync(token);

    public void Update(RefreshToken token) =>
        _context.RefreshTokens.Update(token);

    public Task<RefreshToken?> FindByHashAsync(string tokenHash) =>
        _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

    public Task<RefreshToken?> GetReusableAsync(Guid userId, string securityStamp) =>
        _context.RefreshTokens
            .Where(rt =>
                rt.UserId == userId &&
                rt.SecurityStamp == securityStamp &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .FirstOrDefaultAsync();

    public Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId) =>
        _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
}
