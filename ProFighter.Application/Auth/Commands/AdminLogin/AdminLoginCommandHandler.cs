using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Auth.Commands.Login;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Interfaces.Auth;
using ProFighter.Application.Common.Models.Auth;

namespace ProFighter.Application.Auth.Commands.AdminLogin;

public sealed class AdminLoginCommandHandler : IRequestHandler<AdminLoginCommand, LoginResult>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IFirstLoginTokenService _firstLoginTokenService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AdminLoginCommandHandler> _logger;

    public AdminLoginCommandHandler(
        IAuthenticationService authenticationService,
        IFirstLoginTokenService firstLoginTokenService,
        ITokenService tokenService,
        ILogger<AdminLoginCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _firstLoginTokenService = firstLoginTokenService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        var check = await _authenticationService.ValidateAdminCredentialsAsync(request.MobileNumber, request.Password, cancellationToken);

        if (!check.Succeeded)
            throw new UnauthorizedAccessException("Invalid mobile number or password.");

        if (!check.Roles.Contains("Admin"))
        {
            _logger.LogWarning("User {UserId} attempted admin login but lacks Admin role.", check.UserId);
            throw new UnauthorizedAccessException("Access denied. User does not have admin permissions.");
        }

        if (check.IsFirstLogin)
        {
            var firstLoginToken = _firstLoginTokenService.GenerateToken(request.MobileNumber);
            _logger.LogInformation("First login required for admin user {UserId}, token generated", check.UserId);
            return new LoginResult(RequiresFirstLoginSetup: true, FirstLoginToken: firstLoginToken, JwtToken: null);
        }

        var jwt = await _tokenService.GenerateTokenAsync(
            new TokenGenerationRequest(check.UserId!.Value, check.Roles.ToList(), check.GymType));

        _logger.LogInformation("Admin login successful for user {UserId}", check.UserId);
        return new LoginResult(RequiresFirstLoginSetup: false, FirstLoginToken: null, JwtToken: jwt);
    }
}
