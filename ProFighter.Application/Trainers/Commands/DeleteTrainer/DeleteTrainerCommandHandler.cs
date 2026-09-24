using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Application.Trainers.Commands.DeleteTrainer;

public class DeleteTrainerCommandHandler : IRequestHandler<DeleteTrainerCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DeleteTrainerCommandHandler> _logger;

    public DeleteTrainerCommandHandler(
        IApplicationDbContext context,
        ILogger<DeleteTrainerCommandHandler> logger)
    {
        _context = context;
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
            media.MarkAsDeleted();
        }

        trainer.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft-deleted Trainer {TrainerId} and all associated images.", request.Id);

        return Result<bool>.Success(true, "Trainer and associated images deleted successfully.");
    }
}
