using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Trainers.Common;

namespace ProFighter.Application.Trainers.Commands.UpdateTrainer;

public class UpdateTrainerCommandHandler : IRequestHandler<UpdateTrainerCommand, Result<TrainerDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateTrainerCommandHandler> _logger;

    public UpdateTrainerCommandHandler(IApplicationDbContext context, ILogger<UpdateTrainerCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<TrainerDto>> Handle(UpdateTrainerCommand request, CancellationToken cancellationToken)
    {
        var trainer = await _context.Trainers
            .Include(t => t.Medias)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (trainer == null)
            return Result<TrainerDto>.Failure($"Trainer with ID '{request.Id}' was not found.", 404);

        trainer.UpdateProfile(request.Name, request.Bio, request.GymType, trainingType: request.TrainingType);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated Trainer {TrainerId}", trainer.Id);
        return Result<TrainerDto>.Success(TrainerDto.FromEntity(trainer), "Trainer updated successfully.");
    }
}
