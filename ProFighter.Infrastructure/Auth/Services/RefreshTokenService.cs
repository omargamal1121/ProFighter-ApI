using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Interfaces.Auth;
using ProFighter.Application.Common.Models.Auth;
using ProFighter.Domain.Entities;
using ProFighter.Infrastructure.Identity;

namespace ProFighter.Infrastructure.Auth.Services;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly TimeSpan _refreshTokenExpiry;

    public RefreshTokenService(
        ILogger<RefreshTokenService> logger,
        IConfiguration config,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IApplicationDbContext context)
    {
        _logger      = logger;
        _userManager = userManager;
        _tokenService = tokenService;
        _context     = context;

        var expiryDays      = config.GetValue("Jwt:RefreshTokenExpiryDays", 7);
        _refreshTokenExpiry = TimeSpan.FromDays(expiryDays);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Generate & Store
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<string> GenerateAndStoreAsync(Guid userId, string securityStamp, bool reuseExisting = true)
    {
        _logger.LogInformation("GenerateAndStoreAsync — UserId: {UserId}", userId);

        if (reuseExisting)
        {
            var existingToken = await _context.RefreshTokens
                .Where(rt =>
                    rt.UserId == userId &&
                    rt.SecurityStamp == securityStamp &&
                    !rt.IsRevoked &&
                    rt.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(rt => rt.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingToken != null && !string.IsNullOrWhiteSpace(existingToken.Token))
            {
                _logger.LogInformation("Reusing active refresh token for UserId: {UserId}", userId);
                return existingToken.Token;
            }
        }

        var rawBytes = new byte[64];
        RandomNumberGenerator.Fill(rawBytes);
        var rawToken = Convert.ToBase64String(rawBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        var hash = HashToken(rawToken);

        var entity = new RefreshToken
        {
            UserId        = userId,
            TokenHash     = hash,
            Token         = rawToken,
            SecurityStamp = securityStamp,
            ExpiresAt     = DateTime.UtcNow.Add(_refreshTokenExpiry),
            IsRevoked     = false
        };

        await _context.RefreshTokens.AddAsync(entity);

        _logger.LogInformation("Refresh token stored for UserId: {UserId}", userId);
        return rawToken;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Rotate
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<RefreshTokenResponse> RotateAsync(string rawToken)
    {
        _logger.LogInformation("Executing {Method}", nameof(RotateAsync));

        var hash   = HashToken(rawToken);
        var record = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (record == null)
        {
            _logger.LogWarning("RotateAsync — token not found");
            throw new InvalidOperationException("Invalid refresh token.");
        }

        if (record.IsRevoked)
        {
            _logger.LogWarning("RotateAsync — revoked token replayed. UserId: {UserId}", record.UserId);
            await RevokeAllForUserAsync(record.UserId);
            throw new InvalidOperationException("Refresh token has been revoked.");
        }

        if (DateTime.UtcNow >= record.ExpiresAt)
        {
            _logger.LogWarning("RotateAsync — token expired. UserId: {UserId}", record.UserId);
            throw new InvalidOperationException("Refresh token has expired.");
        }

        var user = await _userManager.FindByIdAsync(record.UserId.ToString());
        if (user == null)
        {
            _logger.LogWarning("RotateAsync — user not found. UserId: {UserId}", record.UserId);
            throw new InvalidOperationException("User not found.");
        }

        if (user.SecurityStamp != record.SecurityStamp)
        {
            _logger.LogWarning("RotateAsync — SecurityStamp mismatch. UserId: {UserId}", record.UserId);
            throw new InvalidOperationException("Session is no longer valid. Please log in again.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("RotateAsync — user is locked out. UserId: {UserId}", record.UserId);
            throw new InvalidOperationException("User account is locked.");
        }

        record.IsRevoked = true;
        record.RevokedAt = DateTime.UtcNow;
        _context.RefreshTokens.Update(record);

        var roles         = (await _userManager.GetRolesAsync(user)).ToList();
        var claims        = (await _userManager.GetClaimsAsync(user)).ToList();
        var accessToken   = await _tokenService.GenerateTokenAsync(new TokenGenerationRequest(user.Id, roles, claims));
        var newRefreshToken = await GenerateAndStoreAsync(user.Id, user.SecurityStamp ?? string.Empty, reuseExisting: false);

        _logger.LogInformation("RotateAsync — rotation successful. UserId: {UserId}", user.Id);

        return new RefreshTokenResponse
        {
            Token        = accessToken,
            RefreshToken = newRefreshToken
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Revoke Single
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<bool> RevokeAsync(string rawToken)
    {
        var hash   = HashToken(rawToken);
        var record = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (record == null || record.IsRevoked)
        {
            _logger.LogInformation("RevokeAsync — token not found or already revoked");
            return true;
        }

        record.IsRevoked = true;
        record.RevokedAt = DateTime.UtcNow;
        _context.RefreshTokens.Update(record);

        _logger.LogInformation("RevokeAsync — token revoked for UserId: {UserId}", record.UserId);
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Revoke All For User
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<bool> RevokeAllForUserAsync(Guid userId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        if (activeTokens.Count == 0)
        {
            _logger.LogInformation("RevokeAllForUserAsync — no active tokens for UserId: {UserId}", userId);
            return true;
        }

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
            _context.RefreshTokens.Update(token);
        }

        _logger.LogInformation("RevokeAllForUserAsync — {Count} token(s) revoked for UserId: {UserId}", activeTokens.Count, userId);
        return true;
    }

    private static string HashToken(string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
