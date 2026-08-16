using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ProFighter.Application.Common.Models.Auth;

namespace ProFighter.Application.Common.Interfaces.Auth;

/// <summary>
/// Single-purpose Identity operations — each method does exactly one thing.
/// Does NOT orchestrate multiple steps. Does NOT call SaveChangesAsync.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Gets the user's identity data by ID.
    /// Throws <see cref="InvalidOperationException"/> if not found.
    /// </summary>
    Task<IdentityUserDto> GetUserByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Validates credentials and returns the user's identity data if valid.
    /// Throws <see cref="UnauthorizedAccessException"/> on invalid credentials or locked account.
    /// </summary>
    Task<IdentityUserDto> ValidateCredentialsAsync(
        string mobileNumber,
        string password,
        CancellationToken ct = default);

    /// <summary>
    /// Removes the existing password and sets a new one.
    /// Throws <see cref="InvalidOperationException"/> on failure.
    /// </summary>
    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Sets the user's email address.
    /// Throws <see cref="InvalidOperationException"/> on failure.
    /// </summary>
    Task SetEmailAsync(Guid userId, string email, CancellationToken ct = default);

    /// <summary>
    /// Calls CompleteAccount() on the user and persists via UserManager.Update.
    /// Throws <see cref="InvalidOperationException"/> on failure.
    /// Returns the refreshed SecurityStamp after the update.
    /// </summary>
    Task<string> MarkAccountCompletedAsync(Guid userId, CancellationToken ct = default);
}

public record IdentityUserDto(
    Guid UserId,
    bool MustCompleteAccount,
    string SecurityStamp,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Claim> Claims);
