using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using System.Threading.Tasks;

namespace ProFighter.Application.Auth.Commands.RequestEmailConfirmation;

public sealed class RequestEmailConfirmationCommandHandler : IRequestHandler<RequestEmailConfirmationCommand, RequestEmailConfirmationResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly ILogger<RequestEmailConfirmationCommandHandler> _logger;

    public RequestEmailConfirmationCommandHandler(
        IApplicationDbContext context,
        IEmailConfirmationService emailConfirmationService,
        ILogger<RequestEmailConfirmationCommandHandler> logger)
    {
        _context = context;
        _emailConfirmationService = emailConfirmationService;
        _logger = logger;
    }

    public async Task<RequestEmailConfirmationResult> Handle(RequestEmailConfirmationCommand request, System.Threading.CancellationToken cancellationToken)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.MobileNumber == request.MobileNumber, cancellationToken);

        // Deliberately do NOT throw/reveal "customer not found" — return generic response
        // to avoid leaking which mobile numbers are registered (standard security practice)
        if (customer is null)
        {
            _logger.LogInformation("Email confirmation requested for unregistered mobile number: {MobileNumber}", request.MobileNumber);
            return new RequestEmailConfirmationResult(false, "If this number is registered, you will receive a confirmation email.");
        }

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            _logger.LogInformation("Email confirmation requested for customer {CustomerId} with no email on file", customer.Id);
            return new RequestEmailConfirmationResult(false, "No email address associated with this account. Please contact support.");
        }

        try
        {
            await _emailConfirmationService.SendConfirmationOtpAsync(customer.Id, cancellationToken);
            _logger.LogInformation("Email confirmation OTP sent successfully to customer {CustomerId}", customer.Id);
            return new RequestEmailConfirmationResult(true, "Email confirmation OTP sent successfully. Please check your email.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email confirmation OTP for customer {CustomerId}", customer.Id);
            return new RequestEmailConfirmationResult(false, "Failed to send confirmation email. Please try again or contact support.");
        }
    }
}
