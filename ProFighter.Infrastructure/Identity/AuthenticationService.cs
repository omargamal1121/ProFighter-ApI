using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Identity;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task<CredentialCheckResult> ValidateCredentialsAsync(string mobileNumber, string password, CancellationToken ct = default)
    {
        var normalizedUserName = mobileNumber.StartsWith("+")
            ? mobileNumber.Substring(1)
            : mobileNumber;

        var user = await _userManager.FindByNameAsync(normalizedUserName);
        if (user == null)
        {
            return new CredentialCheckResult(false, null, false, new List<string>());
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return new CredentialCheckResult(false, null, false, new List<string>());
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == user.Id, ct);
        if (customer == null)
        {
            _logger.LogWarning("Customer not found for ApplicationUser {UserId}", user.Id);
            return new CredentialCheckResult(false, null, false, new List<string>());
        }

        var roles = await _userManager.GetRolesAsync(user);
        return new CredentialCheckResult(true, user.Id, customer.IsFirstLogin, roles.ToList());
    }

    public async Task SetPasswordAndEmailAsync(Guid userId, string newPassword, string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        // Remove existing password and add new one
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to remove password: {errors}");
        }

        var addResult = await _userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
        {
            var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to add password: {errors}");
        }

        // Update email
        user.Email = email;
        user.CompleteAccount(); // Clear MustChangePassword flag

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }

        _logger.LogInformation("Password and email updated successfully for user {UserId}", userId);
    }

    public async Task<IList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> IsEmailConfirmedAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        return await _userManager.IsEmailConfirmedAsync(user);
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        // Generate a reset token and use it to reset the password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to reset password: {errors}");
        }

        // Clear the MustChangePassword flag since the user has now set their own password
        user.CompleteAccount();
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user after password reset: {errors}");
        }

        _logger.LogInformation("Password reset successfully for user {UserId}", userId);
    }

    public async Task ConfirmEmailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to confirm email: {errors}");
        }

        _logger.LogInformation("Email confirmed successfully for user {UserId}", userId);
    }
}
