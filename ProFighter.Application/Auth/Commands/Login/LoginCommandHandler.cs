using MediatR;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Interfaces.Auth;
using ProFighter.Application.Common.Models.Auth;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IFirstLoginTokenService _firstLoginTokenService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthenticationService authenticationService,
        IFirstLoginTokenService firstLoginTokenService,
        ITokenService tokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _firstLoginTokenService = firstLoginTokenService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var check = await _authenticationService.ValidateCredentialsAsync(request.MobileNumber, request.Password, cancellationToken);

        if (!check.Succeeded)
            throw new UnauthorizedAccessException("Invalid mobile number or password.");

        if (check.IsFirstLogin)
        {
            var firstLoginToken = _firstLoginTokenService.GenerateToken(request.MobileNumber);
            _logger.LogInformation("First login required for user {UserId}, token generated", check.UserId);
            return new LoginResult(RequiresFirstLoginSetup: true, FirstLoginToken: firstLoginToken, JwtToken: null);
        }

        var jwt = await _tokenService.GenerateTokenAsync(
            new TokenGenerationRequest(check.UserId!.Value, check.Roles.ToList()));
        _logger.LogInformation("Login successful for user {UserId}", check.UserId);
        return new LoginResult(RequiresFirstLoginSetup: false, FirstLoginToken: null, JwtToken: jwt);
    }
}
