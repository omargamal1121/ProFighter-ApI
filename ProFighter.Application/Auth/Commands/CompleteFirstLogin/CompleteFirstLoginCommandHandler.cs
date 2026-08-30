using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Interfaces.Auth;
using ProFighter.Application.Common.Models.Auth;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Auth.Commands.CompleteFirstLogin;

public sealed class CompleteFirstLoginCommandHandler : IRequestHandler<CompleteFirstLoginCommand, CompleteFirstLoginResult>
{
    private readonly IFirstLoginTokenService _firstLoginTokenService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IIdentityService _identityService;
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly ILogger<CompleteFirstLoginCommandHandler> _logger;

    public CompleteFirstLoginCommandHandler(
        IFirstLoginTokenService firstLoginTokenService,
        IAuthenticationService authenticationService,
        IIdentityService identityService,
        IApplicationDbContext context,
        ITokenService tokenService,
        IEmailConfirmationService emailConfirmationService,
        ILogger<CompleteFirstLoginCommandHandler> logger)
    {
        _firstLoginTokenService = firstLoginTokenService;
        _authenticationService = authenticationService;
        _identityService = identityService;
        _context = context;
        _tokenService = tokenService;
        _emailConfirmationService = emailConfirmationService;
        _logger = logger;
    }

    public async Task<CompleteFirstLoginResult> Handle(CompleteFirstLoginCommand request, CancellationToken cancellationToken)
    {
        var mobileNumber = _firstLoginTokenService.ValidateAndConsumeToken(request.Token);
        if (mobileNumber is null)
            throw new UnauthorizedAccessException("Invalid or expired setup token. Please log in again.");

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.MobileNumber == mobileNumber, cancellationToken)
            ?? throw new InvalidOperationException($"No customer found for mobile number associated with this token.");

        var normalizedEmail = request.Email?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(normalizedEmail))
        {
            var isIdentityUnique = await _identityService.IsEmailUniqueAsync(normalizedEmail, customer.Id, cancellationToken);
            if (!isIdentityUnique)
                throw new InvalidOperationException($"The email address '{request.Email}' is already in use by another account.");

            var isCustomerUnique = !await _context.Customers.AnyAsync(c => c.Email == normalizedEmail && c.Id != customer.Id, cancellationToken);
            if (!isCustomerUnique)
                throw new InvalidOperationException($"The email address '{request.Email}' is already in use by another account.");
        }

        await _authenticationService.SetPasswordAndEmailAsync(customer.Id, request.NewPassword, request.Email, cancellationToken);

        // Update Customer entity with email and mark first login as completed
        try
        {
            customer.UpdateProfile(customer.Name, customer.MobileNumber, normalizedEmail);
            customer.MarkFirstLoginCompleted();
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Duplicate email encountered during Customer update for {CustomerId}.", customer.Id);
            throw new InvalidOperationException($"The email address '{request.Email}' is already in use by another account.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Customer entity for {CustomerId} after successful Identity update. Account is still functional.", customer.Id);
            // Don't throw - the account is still functional with the updated password/email in Identity
        }

        // Best-effort email confirmation send - must not block or fail the login-completion flow
        try
        {
            await _emailConfirmationService.SendConfirmationOtpAsync(customer.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send background email confirmation OTP for customer {CustomerId} after first-login setup.", customer.Id);
            // Intentionally swallowed beyond logging — this must never fail first-login completion.
            // The customer still gets a fully working account; email confirmation can be
            // retried later via the Forgot Password flow's re-send mechanism.
        }

        var roles = await _authenticationService.GetRolesAsync(customer.Id, cancellationToken);
        var jwt = await _tokenService.GenerateTokenAsync(
            new TokenGenerationRequest(customer.Id, roles.ToList()));

        _logger.LogInformation("First login completed successfully for customer {CustomerId}", customer.Id);
        return new CompleteFirstLoginResult(jwt);
    }
}
