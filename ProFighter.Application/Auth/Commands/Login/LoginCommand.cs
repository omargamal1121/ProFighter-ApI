using MediatR;

namespace ProFighter.Application.Auth.Commands.Login;

public record LoginCommand(
    string MobileNumber,
    string Password) : IRequest<LoginResult>;

public record LoginResult(
    bool RequiresFirstLoginSetup,
    string? FirstLoginToken,   // populated only when RequiresFirstLoginSetup is true
    string? JwtToken           // populated only when RequiresFirstLoginSetup is false
);
