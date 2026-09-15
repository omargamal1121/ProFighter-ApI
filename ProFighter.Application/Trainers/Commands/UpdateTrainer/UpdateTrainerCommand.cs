using MediatR;
using ProFighter.Application.Common;
using ProFighter.Application.Trainers.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Trainers.Commands.UpdateTrainer;

/// <summary>Updates trainer text fields only. Use the image endpoints to manage trainer images.</summary>
public record UpdateTrainerCommand(
    Guid Id,
    string Name,
    SubscriptionType Specialization,
    string? Bio,
    GymType? GymType = null
) : IRequest<Result<TrainerDto>>;
