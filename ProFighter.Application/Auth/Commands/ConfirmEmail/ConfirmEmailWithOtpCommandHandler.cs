using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using System.Threading.Tasks;

namespace ProFighter.Application.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailWithOtpCommandHandler : IRequestHandler<ConfirmEmailWithOtpCommand, ConfirmEmailWithOtpResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticationService _authenticationService;
    private readonly IEmailConfirmationOtpService _otpService;
    private readonly ILogger<ConfirmEmailWithOtpCommandHandler> _logger;

    public ConfirmEmailWithOtpCommandHandler(
        IApplicationDbContext context,
        IAuthenticationService authenticationService,
        IEmailConfirmationOtpService otpService,
        ILogger<ConfirmEmailWithOtpCommandHandler> logger)
    {
        _context = context;
        _authenticationService = authenticationService;
        _otpService = otpService;
        _logger = logger;
    }

    public async Task<ConfirmEmailWithOtpResult> Handle(ConfirmEmailWithOtpCommand request, System.Threading.CancellationToken cancellationToken)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.MobileNumber == request.MobileNumber, cancellationToken);

        if (customer is null)
        {
            _logger.LogWarning("Email confirmation OTP attempted for unregistered mobile number: {MobileNumber}", request.MobileNumber);
            return new ConfirmEmailWithOtpResult(false, "Invalid mobile number or OTP.");
        }

        // Validate and consume the OTP
        if (!_otpService.ValidateAndConsumeOtp(customer.Id.ToString(), request.Otp))
        {
            _logger.LogWarning("Invalid or expired OTP for customer {CustomerId}", customer.Id);
            return new ConfirmEmailWithOtpResult(false, "Invalid or expired OTP. Please request a new confirmation code.");
        }

        // Confirm the email
        try
        {
            await _authenticationService.ConfirmEmailAsync(customer.Id, cancellationToken);
            _logger.LogInformation("Email confirmed successfully for customer {CustomerId}", customer.Id);
            return new ConfirmEmailWithOtpResult(true, "Email confirmed successfully. You can now log in.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm email for customer {CustomerId}", customer.Id);
            return new ConfirmEmailWithOtpResult(false, "Failed to confirm email. Please try again or contact support.");
        }
    }
}
