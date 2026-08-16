using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces.Auth;
using ProFighter.Infrastructure.Identity;

namespace ProFighter.Infrastructure.Auth.Services;

/// <summary>
/// Single-purpose Identity operations. Each method performs exactly one action.
/// Does NOT orchestrate multiple steps. Does NOT call SaveChangesAsync.
/// </summary>
public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(UserManager<ApplicationUser> userManager, ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _logger      = logger;
    }

    public async Task<IdentityUserDto> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var roles  = (await _userManager.GetRolesAsync(user)).ToList();
        var claims = (await _userManager.GetClaimsAsync(user)).ToList();

        return new IdentityUserDto(user.Id, user.MustCompleteAccount, user.SecurityStamp ?? string.Empty, roles, claims);
    }

    public async Task<IdentityUserDto> ValidateCredentialsAsync(
        string mobileNumber, string password, CancellationToken ct = default)
    {
        var normalizedUsername = mobileNumber.StartsWith("+") ? mobileNumber[1..] : mobileNumber;

        var user = await _userManager.FindByNameAsync(normalizedUsername);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid mobile number or password.");

        if (!await _userManager.CheckPasswordAsync(user, password))
            throw new UnauthorizedAccessException("Invalid mobile number or password.");

        if (await _userManager.IsLockedOutAsync(user))
            throw new UnauthorizedAccessException("Account is locked.");

        var roles  = (await _userManager.GetRolesAsync(user)).ToList();
        var claims = (await _userManager.GetClaimsAsync(user)).ToList();

        _logger.LogInformation("ValidateCredentialsAsync — success for UserId: {UserId}", user.Id);

        return new IdentityUserDto(user.Id, user.MustCompleteAccount, user.SecurityStamp ?? string.Empty, roles, claims);
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var remove = await _userManager.RemovePasswordAsync(user);
        if (!remove.Succeeded)
            throw new InvalidOperationException(
                $"Failed to remove password: {string.Join("; ", remove.Errors.Select(e => e.Description))}");

        var add = await _userManager.AddPasswordAsync(user, newPassword);
        if (!add.Succeeded)
            throw new InvalidOperationException(
                $"Failed to set new password: {string.Join("; ", add.Errors.Select(e => e.Description))}");

        _logger.LogInformation("ResetPasswordAsync — success for UserId: {UserId}", userId);
    }

    public async Task SetEmailAsync(Guid userId, string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var result = await _userManager.SetEmailAsync(user, email);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to set email: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        _logger.LogInformation("SetEmailAsync — success for UserId: {UserId}", userId);
    }

    public async Task<string> MarkAccountCompletedAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        if (!user.MustCompleteAccount)
            throw new InvalidOperationException("Account is already completed.");

        user.CompleteAccount();
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            throw new InvalidOperationException(
                $"Failed to update user: {string.Join("; ", update.Errors.Select(e => e.Description))}");

        _logger.LogInformation("MarkAccountCompletedAsync — success for UserId: {UserId}", userId);

        // Return refreshed SecurityStamp so the caller can generate a valid refresh token after this update
        return user.SecurityStamp ?? string.Empty;
    }
}
