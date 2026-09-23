using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Trainers.Common;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Trainers.Commands.UploadTrainerImage;

public class UploadTrainerImageCommandHandler : IRequestHandler<UploadTrainerImageCommand, Result<TrainerMediaDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IImageService _imageService;
    private readonly ILogger<UploadTrainerImageCommandHandler> _logger;

    public UploadTrainerImageCommandHandler(
        IApplicationDbContext context,
        IImageService imageService,
        ILogger<UploadTrainerImageCommandHandler> logger)
    {
        _context = context;
        _imageService = imageService;
        _logger = logger;
    }

    public async Task<Result<TrainerMediaDto>> Handle(UploadTrainerImageCommand request, CancellationToken cancellationToken)
    {
        var trainer = await _context.Trainers
            .Include(t => t.Medias)
            .FirstOrDefaultAsync(t => t.Id == request.TrainerId, cancellationToken);

        if (trainer == null)
            return Result<TrainerMediaDto>.Failure($"Trainer with ID '{request.TrainerId}' was not found.", 404);

        var uploadResult = await _imageService.UploadImageAsync(request.Image, "trainers", cancellationToken);
        if (!uploadResult.IsSuccess || uploadResult.Data == null)
            return Result<TrainerMediaDto>.Failure(uploadResult.Message ?? "Failed to upload image.", uploadResult.Status);

        var media = new Media(
            cloudinaryUrl: uploadResult.Data.Url,
            cloudinaryPublicId: uploadResult.Data.PublicId,
            type: MediaType.Image,
            ownerType: MediaOwnerType.Trainer,
            ownerId: trainer.Id,
            purpose: request.Purpose,
            displayOrder: request.DisplayOrder
        );

        trainer.AddMedia(media);
        _context.Medias.Add(media);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded image {MediaId} for Trainer {TrainerId}", media.Id, trainer.Id);
        return Result<TrainerMediaDto>.Success(TrainerMediaDto.FromEntity(media), "Image uploaded successfully.", 201);
    }
}
