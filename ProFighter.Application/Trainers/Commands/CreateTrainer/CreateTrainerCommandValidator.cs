using FluentValidation;

namespace ProFighter.Application.Trainers.Commands.CreateTrainer;

public class CreateTrainerCommandValidator : AbstractValidator<CreateTrainerCommand>
{
    public CreateTrainerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Trainer name is required.")
            .MaximumLength(150).WithMessage("Trainer name cannot exceed 150 characters.");

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio cannot exceed 2000 characters.");
    }
}
