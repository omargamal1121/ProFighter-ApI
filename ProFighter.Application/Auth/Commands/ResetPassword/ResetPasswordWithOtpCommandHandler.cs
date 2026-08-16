using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using System.Threading.Tasks;

namespace ProFighter.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordWithOtpCommandHandler : IRequestHandler<ResetPasswordWithOtpCommand, ResetPasswordWithOtpResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticationService _authenticationService;
    private readonly IPasswordResetOtpService _otpService;
    private readonly ILogger<ResetPasswordWithOtpCommandHandler> _logger;

    public ResetPasswordWithOtpCommandHandler(
        IApplicationDbContext context,
        IAuthenticationService authenticationService,
        IPasswordResetOtpService otpService,
        ILogger<ResetPasswordWithOtpCommandHandler> logger)
    {
        _context = context;
        _authenticationService = authenticationService;
        _otpService = otpService;
        _logger = logger;
    }

    public async Task<ResetPasswordWithOtpResult> Handle(ResetPasswordWithOtpCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.MobileNumber == request.MobileNumber, cancellationToken);

        if (customer is null)
        {
            _logger.LogWarning("Password reset OTP attempted for unregistered mobile number: {MobileNumber}", request.MobileNumber);
            return new ResetPasswordWithOtpResult(false, "Invalid mobile number or OTP.");
        }

        // Validate and consume the OTP
        if (!_otpService.ValidateAndConsumeOtp(customer.Id.ToString(), request.Otp))
        {
            _logger.LogWarning("Invalid or expired OTP for customer {CustomerId}", customer.Id);
            return new ResetPasswordWithOtpResult(false, "Invalid or expired OTP. Please request a new password reset.");
        }

        // Reset the password
        try
        {
            await _authenticationService.ResetPasswordAsync(customer.Id, request.NewPassword, cancellationToken);
            _logger.LogInformation("Password reset successfully for customer {CustomerId}", customer.Id);
            return new ResetPasswordWithOtpResult(true, "Password reset successfully. You can now log in with your new password.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset password for customer {CustomerId}", customer.Id);
            return new ResetPasswordWithOtpResult(false, "Failed to reset password. Please try again or contact support.");
        }
    }
}
