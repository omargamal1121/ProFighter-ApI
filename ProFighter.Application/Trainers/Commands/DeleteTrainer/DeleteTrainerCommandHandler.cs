using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Application.Trainers.Commands.DeleteTrainer;

public class DeleteTrainerCommandHandler : IRequestHandler<DeleteTrainerCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IImageService _imageService;
    private readonly ILogger<DeleteTrainerCommandHandler> _logger;

    public DeleteTrainerCommandHandler(
        IApplicationDbContext context,
        IImageService imageService,
        ILogger<DeleteTrainerCommandHandler> logger)
    {
        _context = context;
        _imageService = imageService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteTrainerCommand request, CancellationToken cancellationToken)
    {
        var trainer = await _context.Trainers
            .Include(t => t.Medias)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (trainer == null)
        {
            return Result<bool>.Failure($"Trainer with ID '{request.Id}' was not found.", 404);
        }

        foreach (var media in trainer.Medias)
        {
            if (!string.IsNullOrEmpty(media.CloudinaryPublicId))
            {
                await _imageService.DeleteImageAsync(media.CloudinaryPublicId, cancellationToken);
            }
            _context.Medias.Remove(media);
        }

        _context.Trainers.Remove(trainer);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted Trainer {TrainerId} and all associated images.", request.Id);

        return Result<bool>.Success(true, "Trainer and associated images deleted successfully.");
    }
}
