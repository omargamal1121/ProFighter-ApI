using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Application.Auth.Commands.ChangeEmail;

public class ChangeEmailCommandHandler : IRequestHandler<ChangeEmailCommand, ChangeEmailResult>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentGymContext _gymContext;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly ILogger<ChangeEmailCommandHandler> _logger;

    public ChangeEmailCommandHandler(
        IAuthenticationService authenticationService,
        IApplicationDbContext context,
        ICurrentGymContext gymContext,
        IEmailConfirmationService emailConfirmationService,
        ILogger<ChangeEmailCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _context = context;
        _gymContext = gymContext;
        _emailConfirmationService = emailConfirmationService;
        _logger = logger;
    }

    public async Task<ChangeEmailResult> Handle(ChangeEmailCommand request, CancellationToken ct)
    {
        var currentGym = _gymContext.CurrentGymType;
        var existingEmail = await _context.Customers
            .AnyAsync(c => c.GymType == currentGym && c.Email == request.NewEmail && c.Id != request.UserId, ct);

        if (existingEmail)
        {
            throw new InvalidOperationException($"A customer with email '{request.NewEmail}' already exists for this gym.");
        }

        await _authenticationService.ChangeEmailAsync(request.UserId, request.NewEmail, ct);

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.UserId, ct);
        if (customer != null)
        {
            customer.UpdateProfile(customer.Name, customer.MobileNumber, request.NewEmail);
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Email updated and marked unconfirmed for customer {CustomerId}", request.UserId);

        await _emailConfirmationService.SendConfirmationOtpAsync(request.UserId, customer.GymType, ct);

        return new ChangeEmailResult(true, "Email changed successfully. Confirmation OTP sent to new email address.");
    }
}
