using MediatR;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Trainers.Common;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Trainers.Commands.CreateTrainer;

public class CreateTrainerCommandHandler : IRequestHandler<CreateTrainerCommand, Result<TrainerDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IImageService _imageService;
    private readonly ILogger<CreateTrainerCommandHandler> _logger;

    public CreateTrainerCommandHandler(
        IApplicationDbContext context,
        IImageService imageService,
        ILogger<CreateTrainerCommandHandler> logger)
    {
        _context = context;
        _imageService = imageService;
        _logger = logger;
    }

    public async Task<Result<TrainerDto>> Handle(CreateTrainerCommand request, CancellationToken cancellationToken)
    {
        var trainerId = Guid.NewGuid();

        // Trainer starts INACTIVE if no image is provided
        bool hasImage = request.Image != null && request.Image.Length > 0;
        bool isActive = hasImage;

        var trainer = new Trainer(
            id: trainerId,
            name: request.Name,
            specialization: request.Specialization,
            bio: request.Bio,
            isActive: isActive,
            gymType: request.GymType
        );

        _context.Trainers.Add(trainer);

        if (hasImage)
        {
            var uploadResult = await _imageService.UploadImageAsync(request.Image!, "trainers", cancellationToken);
            if (!uploadResult.IsSuccess || uploadResult.Data == null)
            {
                return Result<TrainerDto>.Failure(
                    uploadResult.Message ?? "Failed to upload trainer image.", uploadResult.Status);
            }

            var media = new Media(
                id: Guid.NewGuid(),
                cloudinaryUrl: uploadResult.Data.Url,
                cloudinaryPublicId: uploadResult.Data.PublicId,
                type: MediaType.Image,
                ownerType: MediaOwnerType.Trainer,
                ownerId: trainerId,
                purpose: MediaPurpose.ProfileImage,
                displayOrder: 0
            );

            trainer.AddMedia(media);
            _context.Medias.Add(media);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created Trainer {TrainerId} '{Name}' — IsActive: {IsActive}", trainer.Id, trainer.Name, trainer.IsActive);

        var msg = isActive
            ? "Trainer created and activated successfully."
            : "Trainer created but is inactive. Upload an image then activate the trainer.";

        return Result<TrainerDto>.Success(TrainerDto.FromEntity(trainer), msg, 201);
    }
}
