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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTrainerCommandHandler> _logger;

    public CreateTrainerCommandHandler(
        IApplicationDbContext context,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<CreateTrainerCommandHandler> logger)
    {
        _context = context;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TrainerDto>> Handle(CreateTrainerCommand request, CancellationToken cancellationToken)
    {
        bool hasImage = request.Image != null && request.Image.Length > 0;

        // Step 1: Persist the trainer row. IsActive starts false; it is set true once the image is confirmed saved.
        var trainer = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
           
            var t = new Trainer(
                name: request.Name,
                bio: request.Bio,
                isActive: false,
                gymType: request.GymType
            );
            _context.Trainers.Add(t);
            return t;
        }, cancellationToken);

      
        if (hasImage)
        {
            var uploadResult = await _imageService.UploadImageAsync(request.Image!, "trainers", cancellationToken);
            if (!uploadResult.IsSuccess || uploadResult.Data == null)
            {
                return Result<TrainerDto>.Failure(
                    uploadResult.Message ?? "Failed to upload trainer image.", uploadResult.Status);
            }

            var uploadedPublicId = uploadResult.Data.PublicId;
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    var media = Media.ForTrainer(
                        trainerId: trainer.Id,
                        cloudinaryUrl: uploadResult.Data.Url,
                        cloudinaryPublicId: uploadedPublicId,
                        type: MediaType.Image,
                        purpose: MediaPurpose.ProfileImage,
                        displayOrder: 0
                    );

                    trainer.AddMedia(media);
                    trainer.Activate();
                    _context.Medias.Add(media);

                    return true;
                }, cancellationToken);
            }
            catch
            {
                await _imageService.DeleteImageAsync(uploadedPublicId, cancellationToken);
                throw;
            }
        }

    
        _logger.LogInformation(
            "Created Trainer {TrainerId} '{Name}' — IsActive: {IsActive}",
            trainer.Id, trainer.Name, trainer.IsActive);

        var msg = hasImage
            ? "Trainer created and activated successfully."
            : "Trainer created but is inactive. Upload an image then activate the trainer.";

        return Result<TrainerDto>.Success(TrainerDto.FromEntity(trainer), msg, 201);
    }
}
