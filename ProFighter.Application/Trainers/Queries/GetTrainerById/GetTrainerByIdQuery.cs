using MediatR;
using ProFighter.Application.Common;
using ProFighter.Application.Trainers.Common;

namespace ProFighter.Application.Trainers.Queries.GetTrainerById;

public record GetTrainerByIdQuery(Guid Id) : IRequest<Result<TrainerDto>>;
