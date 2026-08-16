using MediatR;

namespace ProFighter.Application.Auth.Commands.CompleteFirstLogin;

public record CompleteFirstLoginCommand(
    string Token,
    string NewPassword,
    string Email) : IRequest<CompleteFirstLoginResult>;

public record CompleteFirstLoginResult(string JwtToken);
