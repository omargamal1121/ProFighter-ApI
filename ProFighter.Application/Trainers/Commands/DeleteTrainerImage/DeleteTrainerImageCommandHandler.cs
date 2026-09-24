using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Application.Trainers.Commands.DeleteTrainerImage;

public class DeleteTrainerImageCommandHandler : IRequestHandler<DeleteTrainerImageCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DeleteTrainerImageCommandHandler> _logger;

    public DeleteTrainerImageCommandHandler(
        IApplicationDbContext context,
        ILogger<DeleteTrainerImageCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteTrainerImageCommand request, CancellationToken cancellationToken)
    {
        var trainer = await _context.Trainers
            .Include(t => t.Medias)
            .FirstOrDefaultAsync(t => t.Id == request.TrainerId, cancellationToken);

        if (trainer == null)
            return Result<bool>.Failure($"Trainer with ID '{request.TrainerId}' was not found.", 404);

        var media = trainer.Medias.FirstOrDefault(m => m.Id == request.ImageId);
        if (media == null)
            return Result<bool>.Failure($"Image with ID '{request.ImageId}' was not found for this trainer.", 404);

        // Soft-delete the media entry (keeps DB FK intact to satisfy CK_Media_SingleOwner)
        media.MarkAsDeleted();

        // Count remaining active (non-soft-deleted) images
        var remainingImages = trainer.Medias.Count(m => !m.IsDeleted && m.Id != request.ImageId);
        if (remainingImages == 0 && trainer.IsActive)
        {
            trainer.Deactivate();
            _logger.LogWarning(
                "Trainer {TrainerId} auto-deactivated: no images remaining after deleting image {ImageId}",
                trainer.Id, request.ImageId);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var message = remainingImages == 0
            ? "Image deleted. Trainer has been deactivated because no images remain."
            : "Trainer image deleted successfully.";

        _logger.LogInformation("Soft-deleted image {ImageId} for Trainer {TrainerId}", request.ImageId, request.TrainerId);
        return Result<bool>.Success(true, message);
    }
}
