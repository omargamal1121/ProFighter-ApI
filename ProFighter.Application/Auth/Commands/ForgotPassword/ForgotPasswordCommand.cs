using MediatR;

namespace ProFighter.Application.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string MobileNumber) : IRequest<ForgotPasswordResult>;

public record ForgotPasswordResult(ForgotPasswordOutcome Outcome);

public enum ForgotPasswordOutcome
{
    ResetOtpSent,          // email was confirmed, password reset OTP sent
    EmailNotConfirmed,       // email was NOT confirmed — user must confirm email first via separate endpoint
    NoEmailOnFile            // customer has no email at all — cannot proceed via email; front-end should direct to support/alternate recovery
}
