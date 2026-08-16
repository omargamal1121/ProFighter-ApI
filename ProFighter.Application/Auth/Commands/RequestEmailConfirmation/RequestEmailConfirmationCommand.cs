using MediatR;

namespace ProFighter.Application.Auth.Commands.RequestEmailConfirmation;

public record RequestEmailConfirmationCommand(string MobileNumber) : IRequest<RequestEmailConfirmationResult>;

public record RequestEmailConfirmationResult(bool Success, string Message);
