using FluentValidation;
using ProFighter.Application.Common.Constants;

namespace ProFighter.Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(v => v.MobileNumber)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(ValidationPatterns.SaudiMobileNumberPattern)
            .WithMessage("Mobile number must be in the format 966XXXXXXXXX (12 digits, starting with 966, no + or spaces).");
    }
}
