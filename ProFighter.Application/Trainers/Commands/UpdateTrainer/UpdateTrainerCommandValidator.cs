using FluentValidation;

namespace ProFighter.Application.Trainers.Commands.UpdateTrainer;

public class UpdateTrainerCommandValidator : AbstractValidator<UpdateTrainerCommand>
{
    public UpdateTrainerCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Trainer ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Trainer name is required.")
            .MaximumLength(150).WithMessage("Trainer name cannot exceed 150 characters.");

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio cannot exceed 2000 characters.");
    }
}
