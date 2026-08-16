namespace ProFighter.Application.Common.Interfaces;

public interface IEmailConfirmationOtpService
{
    void StoreOtp(string userId, string otp, TimeSpan expiry);
    bool ValidateAndConsumeOtp(string userId, string otp);
}
