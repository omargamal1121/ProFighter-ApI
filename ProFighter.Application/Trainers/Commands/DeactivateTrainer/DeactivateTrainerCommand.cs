using MediatR;
using ProFighter.Application.Common;
using ProFighter.Application.Trainers.Common;

namespace ProFighter.Application.Trainers.Commands.DeactivateTrainer;

public record DeactivateTrainerCommand(Guid Id) : IRequest<Result<TrainerDto>>;
