using FluentValidation;
using ProFighter.Application.Common.Constants;

namespace ProFighter.Application.Customers.Commands.RegisterCustomer;

public class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(v => v.MobileNumber)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(ValidationPatterns.SaudiMobileNumberPattern)
            .WithMessage("Mobile number must be in the format 966XXXXXXXXX (12 digits, starting with 966, no + or spaces).");

        RuleFor(v => v.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.")
            .When(v => !string.IsNullOrEmpty(v.Email));

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
            // Note: ASP.NET Identity's PasswordOptions is the authoritative check at UserManager.CreateAsync time.
    }
}
