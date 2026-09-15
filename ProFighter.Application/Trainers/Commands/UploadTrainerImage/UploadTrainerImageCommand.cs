using MediatR;
using Microsoft.AspNetCore.Http;
using ProFighter.Application.Common;
using ProFighter.Application.Trainers.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Trainers.Commands.UploadTrainerImage;

public record UploadTrainerImageCommand(
    Guid TrainerId,
    IFormFile Image,
    MediaPurpose Purpose = MediaPurpose.Gallery,
    int DisplayOrder = 0
) : IRequest<Result<TrainerMediaDto>>;
