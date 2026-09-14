using FluentValidation;

namespace ProFighter.Application.Subscriptions.Commands.CreateSubscription;

public class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(v => v.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(v => v.PlanName)
            .NotEmpty().WithMessage("Plan name is required.");

        RuleFor(v => v.PriceId)
            .NotEmpty().WithMessage("Price ID is required.");

        RuleFor(v => v.Quantity)
            .GreaterThanOrEqualTo(1).WithMessage("Quantity must be at least 1.");
    }
}
