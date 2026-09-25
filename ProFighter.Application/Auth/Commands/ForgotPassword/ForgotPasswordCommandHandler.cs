using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Extensions;
using ProFighter.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticationService _authenticationService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly ICurrentGymContext _gymContext;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IAuthenticationService authenticationService,
        IPasswordResetService passwordResetService,
        ICurrentGymContext gymContext,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _context = context;
        _authenticationService = authenticationService;
        _passwordResetService = passwordResetService;
        _gymContext = gymContext;
        _logger = logger;
    }

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var customer = await _context.Customers
            .ForCurrentGym(_gymContext)
            .FirstOrDefaultAsync(c => c.MobileNumber == request.MobileNumber, ct);

     
        if (customer is null)
        {
            _logger.LogInformation("Forgot-password requested for unregistered mobile number.");
            return new ForgotPasswordResult(ForgotPasswordOutcome.AccountNotFound, "Account not found with this mobile number.");
        }

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            return new ForgotPasswordResult(ForgotPasswordOutcome.NoEmailOnFile, "No email address found on file for this account.");
        }

        var isConfirmed = await _authenticationService.IsEmailConfirmedAsync(customer.Id, ct);

        if (!isConfirmed)
        {
            _logger.LogInformation("Password reset requested for customer {CustomerId} with unconfirmed email", customer.Id);
            return new ForgotPasswordResult(ForgotPasswordOutcome.EmailNotConfirmed, "Email address is not confirmed. Please confirm your email first.");
        }

        try
        {
            var otpResult = await _passwordResetService.SendPasswordResetOtpAsync(customer.Id, customer.GymType, ct);
            if (otpResult == Common.Enums.PasswordResetOtpResult.EmailConfirmationRequired)
            {
                return new ForgotPasswordResult(ForgotPasswordOutcome.EmailNotConfirmed,"Confirm your email first check your dm");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset OTP for customer {CustomerId}.", customer.Id);
           
        }

        return new ForgotPasswordResult(ForgotPasswordOutcome.ResetOtpSent, "Password reset OTP sent successfully to your email.");
    }
}
