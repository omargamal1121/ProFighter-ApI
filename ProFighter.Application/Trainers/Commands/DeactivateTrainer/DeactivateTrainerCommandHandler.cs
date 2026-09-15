using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Trainers.Common;

namespace ProFighter.Application.Trainers.Commands.DeactivateTrainer;

public class DeactivateTrainerCommandHandler : IRequestHandler<DeactivateTrainerCommand, Result<TrainerDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DeactivateTrainerCommandHandler> _logger;

    public DeactivateTrainerCommandHandler(IApplicationDbContext context, ILogger<DeactivateTrainerCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<TrainerDto>> Handle(DeactivateTrainerCommand request, CancellationToken cancellationToken)
    {
        var trainer = await _context.Trainers
            .Include(t => t.Medias)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (trainer == null)
            return Result<TrainerDto>.Failure($"Trainer with ID '{request.Id}' was not found.", 404);

        if (!trainer.IsActive)
            return Result<TrainerDto>.Failure("Trainer is already inactive.", 409);

        trainer.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated Trainer {TrainerId}", trainer.Id);
        return Result<TrainerDto>.Success(TrainerDto.FromEntity(trainer), "Trainer deactivated successfully.");
    }
}
