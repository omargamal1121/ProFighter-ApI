using MediatR;

namespace ProFighter.Application.Auth.Commands.ChangeEmail;

public record ChangeEmailCommand(Guid UserId, string NewEmail) : IRequest<ChangeEmailResult>;

public record ChangeEmailResult(bool Success, string Message);
