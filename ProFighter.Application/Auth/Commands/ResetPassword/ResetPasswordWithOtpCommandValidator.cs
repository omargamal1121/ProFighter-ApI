using FluentValidation;
using ProFighter.Application.Common.Constants;

namespace ProFighter.Application.Auth.Commands.ResetPassword;

public class ResetPasswordWithOtpCommandValidator : AbstractValidator<ResetPasswordWithOtpCommand>
{
    public ResetPasswordWithOtpCommandValidator()
    {
        RuleFor(v => v.MobileNumber)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(ValidationPatterns.SaudiMobileNumberPattern)
            .WithMessage("Mobile number must be in the format 966XXXXXXXXX (12 digits, starting with 966, no + or spaces).");

        RuleFor(v => v.Otp)
            .NotEmpty().WithMessage("OTP is required.")
            .Matches(@"^\d{6}$").WithMessage("OTP must be exactly 6 digits.");

        RuleFor(v => v.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
    }
}
