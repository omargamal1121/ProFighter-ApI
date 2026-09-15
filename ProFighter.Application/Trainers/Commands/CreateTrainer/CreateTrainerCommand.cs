using MediatR;
using Microsoft.AspNetCore.Http;
using ProFighter.Application.Common;
using ProFighter.Application.Trainers.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Trainers.Commands.CreateTrainer;

public record CreateTrainerCommand(
    string Name,
    SubscriptionType Specialization,
    string? Bio,
    GymType GymType = GymType.ProFighter,
    IFormFile? Image = null
) : IRequest<Result<TrainerDto>>;
