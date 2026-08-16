using FluentValidation;

namespace ProFighter.Application.Auth.Commands.CompleteFirstLogin;

public class CompleteFirstLoginCommandValidator : AbstractValidator<CompleteFirstLoginCommand>
{
    public CompleteFirstLoginCommandValidator()
    {
        RuleFor(v => v.Token)
            .NotEmpty().WithMessage("Token is required.");

        RuleFor(v => v.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");
    }
}
