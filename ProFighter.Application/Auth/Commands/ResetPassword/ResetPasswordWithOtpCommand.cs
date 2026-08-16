using MediatR;

namespace ProFighter.Application.Auth.Commands.ResetPassword;

public record ResetPasswordWithOtpCommand(
    string MobileNumber,
    string Otp,
    string NewPassword) : IRequest<ResetPasswordWithOtpResult>;

public record ResetPasswordWithOtpResult(bool Success, string Message);
