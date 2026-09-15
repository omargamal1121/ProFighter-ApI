using MediatR;
using Microsoft.EntityFrameworkCore;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Trainers.Common;

namespace ProFighter.Application.Trainers.Queries.GetTrainerById;

public class GetTrainerByIdQueryHandler : IRequestHandler<GetTrainerByIdQuery, Result<TrainerDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTrainerByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<TrainerDto>> Handle(GetTrainerByIdQuery request, CancellationToken cancellationToken)
    {
        var trainer = await _context.Trainers
            .Include(t => t.Medias)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (trainer == null)
        {
            return Result<TrainerDto>.Failure($"Trainer with ID '{request.Id}' was not found.", 404);
        }

        return Result<TrainerDto>.Success(TrainerDto.FromEntity(trainer), "Trainer retrieved successfully.");
    }
}
