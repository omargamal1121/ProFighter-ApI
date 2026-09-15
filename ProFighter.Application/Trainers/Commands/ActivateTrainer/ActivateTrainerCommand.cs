using MediatR;
using ProFighter.Application.Common;
using ProFighter.Application.Trainers.Common;

namespace ProFighter.Application.Trainers.Commands.ActivateTrainer;

public record ActivateTrainerCommand(Guid Id) : IRequest<Result<TrainerDto>>;
