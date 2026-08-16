using MediatR;

namespace ProFighter.Application.Auth.Commands.ConfirmEmail;

public record ConfirmEmailWithOtpCommand(
    string MobileNumber,
    string Otp) : IRequest<ConfirmEmailWithOtpResult>;

public record ConfirmEmailWithOtpResult(bool Success, string Message);
