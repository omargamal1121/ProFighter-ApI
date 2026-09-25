using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Threading;

namespace ProFighter.Infrastructure.Identity;

public class EmailConfirmationService : IEmailConfirmationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountEmailService _accountEmailService;
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailConfirmationService> _logger;
    private readonly IEmailConfirmationOtpService _otpService;

    public EmailConfirmationService(
        UserManager<ApplicationUser> userManager,
        IAccountEmailService accountEmailService,
        IApplicationDbContext context,
        IConfiguration configuration,
        ILogger<EmailConfirmationService> logger,
        IEmailConfirmationOtpService otpService)
    {
        _userManager = userManager;
        _accountEmailService = accountEmailService;
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _otpService = otpService;
    }

    public async Task SendConfirmationOtpAsync(Guid customerId, ProFighter.Domain.Enums.GymType? gymType = null, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(customerId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {customerId} not found.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException($"User {customerId} does not have an email address.");
        }

        // Generate a 6-digit OTP
        var otp = GenerateOtp();
        var expiryMinutes = int.TryParse(_configuration["Auth:EmailConfirmationOtpExpiryMinutes"], out var minutes)
            ? minutes
            : 15;

        // Store OTP in cache
        _otpService.StoreOtp(customerId.ToString(), otp, TimeSpan.FromMinutes(expiryMinutes));

        // Send OTP via email
        var mobileNumber = !string.IsNullOrWhiteSpace(user.PhoneNumber) ? user.PhoneNumber : user.UserName;
        await _accountEmailService.SendValidationEmailAsync(user.Email, user.Id.ToString(), otp, mobileNumber, gymType);
        _logger.LogInformation("Email confirmation OTP sent successfully to customer {CustomerId}", customerId);
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
