using MediatR;
using ProFighter.Application.Common;
using ProFighter.Application.Trainers.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Trainers.Queries.GetTrainers;

public record GetTrainersQuery(
    GymType? GymType = null,
    bool? IsActive = null,
    string? SearchTerm = null
) : IRequest<Result<List<TrainerDto>>>;
