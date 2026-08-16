using FluentValidation;
using ProFighter.Application.Common.Constants;

namespace ProFighter.Application.Auth.Commands.ConfirmEmail;

public class ConfirmEmailWithOtpCommandValidator : AbstractValidator<ConfirmEmailWithOtpCommand>
{
    public ConfirmEmailWithOtpCommandValidator()
    {
        RuleFor(v => v.MobileNumber)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(ValidationPatterns.SaudiMobileNumberPattern)
            .WithMessage("Mobile number must be in the format 966XXXXXXXXX (12 digits, starting with 966, no + or spaces).");

        RuleFor(v => v.Otp)
            .NotEmpty().WithMessage("OTP is required.")
            .Matches(@"^\d{6}$").WithMessage("OTP must be exactly 6 digits.");
    }
}
