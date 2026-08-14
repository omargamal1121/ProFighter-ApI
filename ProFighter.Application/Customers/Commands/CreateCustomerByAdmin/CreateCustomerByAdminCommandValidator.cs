using FluentValidation;

namespace ProFighter.Application.Customers.Commands.CreateCustomerByAdmin;

public class CreateCustomerByAdminCommandValidator : AbstractValidator<CreateCustomerByAdminCommand>
{
    public CreateCustomerByAdminCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

        RuleFor(v => v.MobileNumber)
            .NotEmpty().WithMessage("Mobile number is required.")
            .MaximumLength(20).WithMessage("Mobile number must not exceed 20 characters.");

        RuleFor(v => v.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.")
            .When(v => !string.IsNullOrEmpty(v.Email));
    }
}
