using MediatR;
using ProFighter.Application.Common;

namespace ProFighter.Application.Trainers.Commands.DeleteTrainerImage;

public record DeleteTrainerImageCommand(
    Guid TrainerId,
    Guid ImageId
) : IRequest<Result<bool>>;
