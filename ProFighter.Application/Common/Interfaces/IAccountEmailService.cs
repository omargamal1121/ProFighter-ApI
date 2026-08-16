namespace ProFighter.Application.Common.Interfaces;

public interface IAccountEmailService
{
    Task SendValidationEmailAsync(string email, string userId, string otp);
    Task SendPasswordResetEmailAsync(string email, string username, string otp);
    Task SendPasswordResetSuccessEmailAsync(string email);
    Task SendAccountLockedEmailAsync(string email, string username, string reason = "Multiple failed login attempts");
    Task SendWelcomeEmailAsync(string email, string username);
    Task SendEmailAfterChangePassAsync(string username, string email);
}
