using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticationService _authenticationService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IAuthenticationService authenticationService,
        IPasswordResetService passwordResetService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _context = context;
        _authenticationService = authenticationService;
        _passwordResetService = passwordResetService;
        _logger = logger;
    }

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.MobileNumber == request.MobileNumber, ct);

        // Deliberately do NOT throw/reveal "customer not found" — return the same
        // NoEmailOnFile-shaped response to avoid leaking which mobile numbers are registered
        // (standard forgot-password enumeration-prevention practice). Log internally for visibility.
        if (customer is null)
        {
            _logger.LogInformation("Forgot-password requested for unregistered mobile number.");
            return new ForgotPasswordResult(ForgotPasswordOutcome.NoEmailOnFile);
        }

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            return new ForgotPasswordResult(ForgotPasswordOutcome.NoEmailOnFile);
        }

        var isConfirmed = await _authenticationService.IsEmailConfirmedAsync(customer.Id, ct);

        if (!isConfirmed)
        {
            _logger.LogInformation("Password reset requested for customer {CustomerId} with unconfirmed email", customer.Id);
            return new ForgotPasswordResult(ForgotPasswordOutcome.EmailNotConfirmed);
        }

        try
        {
            await _passwordResetService.SendPasswordResetOtpAsync(customer.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset OTP for customer {CustomerId}.", customer.Id);
            // Still report success-shaped outcome to the caller for the same enumeration-prevention
            // reason as above — the failure is logged internally for ops follow-up.
        }

        return new ForgotPasswordResult(ForgotPasswordOutcome.ResetOtpSent);
    }
}
