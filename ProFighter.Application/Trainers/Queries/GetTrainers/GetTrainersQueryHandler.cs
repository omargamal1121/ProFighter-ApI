using MediatR;
using Microsoft.EntityFrameworkCore;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Trainers.Common;

namespace ProFighter.Application.Trainers.Queries.GetTrainers;

public class GetTrainersQueryHandler : IRequestHandler<GetTrainersQuery, Result<List<TrainerDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetTrainersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<TrainerDto>>> Handle(GetTrainersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Trainers
            .Include(t => t.Medias)
            .AsNoTracking()
            .AsQueryable();

        if (request.GymType.HasValue)
        {
            query = query.Where(t => t.GymType == request.GymType.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(search) || (t.Bio != null && t.Bio.ToLower().Contains(search)));
        }

        var trainers = await query
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        var dtos = trainers.Select(TrainerDto.FromEntity).ToList();

        return Result<List<TrainerDto>>.Success(dtos, "Trainers retrieved successfully.");
    }
}
