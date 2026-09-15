using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Trainers.Common;

namespace ProFighter.Application.Trainers.Commands.ActivateTrainer;

public class ActivateTrainerCommandHandler : IRequestHandler<ActivateTrainerCommand, Result<TrainerDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ActivateTrainerCommandHandler> _logger;

    public ActivateTrainerCommandHandler(IApplicationDbContext context, ILogger<ActivateTrainerCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<TrainerDto>> Handle(ActivateTrainerCommand request, CancellationToken cancellationToken)
    {
        var trainer = await _context.Trainers
            .Include(t => t.Medias)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (trainer == null)
            return Result<TrainerDto>.Failure($"Trainer with ID '{request.Id}' was not found.", 404);

        if (trainer.IsActive)
            return Result<TrainerDto>.Failure("Trainer is already active.", 409);

        // Business rule: cannot activate a trainer without at least one image
        if (!trainer.Medias.Any())
            return Result<TrainerDto>.Failure(
                "Cannot activate trainer: trainer must have at least one image before being activated.", 422);

        trainer.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated Trainer {TrainerId}", trainer.Id);
        return Result<TrainerDto>.Success(TrainerDto.FromEntity(trainer), "Trainer activated successfully.");
    }
}
