using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Infrastructure.Identity;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ICurrentGymContext _gymContext;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        ILogger<AuthenticationService> logger,
        ICurrentGymContext gymContext)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
        _gymContext = gymContext;
    }

    public async Task<CredentialCheckResult> ValidateCredentialsAsync(string mobileNumber, string password, CancellationToken ct = default)
    {
        var gymType = _gymContext.CurrentGymType;

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.MobileNumber == mobileNumber && c.GymType == gymType, ct);

        if (customer == null)
        {
            return new CredentialCheckResult(false, null, false, new List<string>());
        }

        var user = await _userManager.FindByIdAsync(customer.Id.ToString());
        if (user == null)
        {
            _logger.LogWarning("Identity user not found for Customer {CustomerId}", customer.Id);
            return new CredentialCheckResult(false, null, false, new List<string>());
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return new CredentialCheckResult(false, null, false, new List<string>());
        }

        var roles = await _userManager.GetRolesAsync(user);
        return new CredentialCheckResult(true, user.Id, customer.IsFirstLogin, roles.ToList(), customer.GymType);
    }

    public async Task<CredentialCheckResult> ValidateAdminCredentialsAsync(string mobileNumber, string password, CancellationToken ct = default)
    {
        var sanitizedMobile = new string(mobileNumber.Where(char.IsDigit).ToArray());

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => (c.MobileNumber == mobileNumber || c.MobileNumber == sanitizedMobile)&&c.GymType==0, ct);

        ApplicationUser? user = null;
        if (customer != null)
        {
            user = await _userManager.FindByIdAsync(customer.Id.ToString());
        }
        else
        {
            var username = $"{sanitizedMobile}_0";
            user = await _userManager.FindByNameAsync(username)
                ?? await _userManager.FindByNameAsync(mobileNumber)
                ?? await _userManager.FindByEmailAsync(mobileNumber);

            if (user != null)
            {
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == user.Id, ct);
            }
        }

        if (user == null)
        {
            return new CredentialCheckResult(false, null, false, new List<string>());
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return new CredentialCheckResult(false, null, false, new List<string>());
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isFirstLogin = customer?.IsFirstLogin ?? user.MustChangePassword;
        var gymType = customer?.GymType ?? GymType.ProFighter;

        return new CredentialCheckResult(true, user.Id, isFirstLogin, roles.ToList(), gymType);
    }


    public async Task SetPasswordAndEmailAsync(Guid userId, string newPassword, string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        // Validate new password FIRST before removing or modifying existing password
        foreach (var validator in _userManager.PasswordValidators)
        {
            var valResult = await validator.ValidateAsync(_userManager, user, newPassword);
            if (!valResult.Succeeded)
            {
                var errors = string.Join("; ", valResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Password validation failed: {errors}");
            }
        }

        // Safely set password using ResetPasswordAsync (or AddPasswordAsync if user has no password)
        if (await _userManager.HasPasswordAsync(user))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to set password: {errors}");
            }
        }
        else
        {
            var addResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (!addResult.Succeeded)
            {
                var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to set password: {errors}");
            }
        }

        // Update email
        user.Email = email;
        user.CompleteAccount(); // Clear MustChangePassword flag

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user profile: {errors}");
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

        if (user.EmailConfirmed)
        {
            _logger.LogInformation("Email already confirmed for user {UserId}", userId);
            return;
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

    public async Task ChangeEmailAsync(Guid userId, string newEmail, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        user.Email = newEmail;
        user.EmailConfirmed = false;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update email: {errors}");
        }

        _logger.LogInformation("Email updated to {NewEmail} and marked unconfirmed for user {UserId}", newEmail, userId);
    }
}
