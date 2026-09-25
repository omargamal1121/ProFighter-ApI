using MediatR;

namespace ProFighter.Application.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string MobileNumber) : IRequest<ForgotPasswordResult>;

public record ForgotPasswordResult(ForgotPasswordOutcome Outcome, string Message);

public enum ForgotPasswordOutcome
{
    ResetOtpSent,
    EmailNotConfirmed,
    NoEmailOnFile,
    AccountNotFound
}
