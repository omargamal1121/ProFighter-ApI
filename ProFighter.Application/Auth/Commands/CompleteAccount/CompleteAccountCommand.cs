using MediatR;

namespace ProFighter.Application.Auth.Commands.CompleteAccount;

public record CompleteAccountCommand(
    Guid UserId,
    string NewPassword,
    string Email) : IRequest<CompleteAccountResult>;

public record CompleteAccountResult(
    string Token,
    string RefreshToken);
