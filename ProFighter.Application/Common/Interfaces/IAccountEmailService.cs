namespace ProFighter.Application.Common.Interfaces;

public interface IAccountEmailService
{
    Task SendValidationEmailAsync(string email, string userId, string otp, string? mobileNumber = null, Domain.Enums.GymType? gymType = null);
    Task SendPasswordResetEmailAsync(string email, string username, string otp, string? mobileNumber = null, Domain.Enums.GymType? gymType = null);
    Task SendPasswordResetSuccessEmailAsync(string email);
    Task SendAccountLockedEmailAsync(string email, string username, string reason = "Multiple failed login attempts");
    Task SendWelcomeEmailAsync(string email, string username);
    Task SendEmailAfterChangePassAsync(string username, string email);
}

