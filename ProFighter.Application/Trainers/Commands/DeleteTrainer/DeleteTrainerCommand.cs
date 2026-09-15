using MediatR;
using ProFighter.Application.Common;

namespace ProFighter.Application.Trainers.Commands.DeleteTrainer;

public record DeleteTrainerCommand(Guid Id) : IRequest<Result<bool>>;
