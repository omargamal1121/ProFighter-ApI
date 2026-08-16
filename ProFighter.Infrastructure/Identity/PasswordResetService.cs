using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Infrastructure.Identity;

public class PasswordResetService : IPasswordResetService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountEmailService _accountEmailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly IPasswordResetOtpService _otpService;

    public PasswordResetService(
        UserManager<ApplicationUser> userManager,
        IAccountEmailService accountEmailService,
        IConfiguration configuration,
        ILogger<PasswordResetService> logger,
        IPasswordResetOtpService otpService)
    {
        _userManager = userManager;
        _accountEmailService = accountEmailService;
        _configuration = configuration;
        _logger = logger;
        _otpService = otpService;
    }

    public async Task SendPasswordResetOtpAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException($"User {userId} does not have an email address.");
        }

        // Generate a 6-digit OTP
        var otp = GenerateOtp();
        var expiryMinutes = int.TryParse(_configuration["Auth:PasswordResetOtpExpiryMinutes"], out var minutes)
            ? minutes
            : 15;

        // Store OTP in cache
        _otpService.StoreOtp(userId.ToString(), otp, TimeSpan.FromMinutes(expiryMinutes));

        // Send OTP via email
        await _accountEmailService.SendPasswordResetEmailAsync(user.Email, user.UserName, otp);
        _logger.LogInformation("Password reset OTP sent successfully to customer {CustomerId}", userId);
    }

    private static string GenerateOtp()
    {
        // Generate a 6-digit OTP
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var otp = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return otp.ToString("D6"); // Pad with leading zeros if needed
    }
}
