using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Application.Subscriptions.Queries.GetMySubscriptions;

public class GetMySubscriptionsQueryHandler
    : IRequestHandler<GetMySubscriptionsQuery, GetMySubscriptionsResult>
{
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetMySubscriptionsQueryHandler> _logger;

    public GetMySubscriptionsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetMySubscriptionsQueryHandler> logger)
    {
        _context = context;
        _logger  = logger;
    }

    public async Task<GetMySubscriptionsResult> Handle(
        GetMySubscriptionsQuery request,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var page     = Math.Max(request.Page, 1);

        var query = _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.CustomerId == request.CustomerId);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(s => s.Status == request.Status);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(s => s.EndDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new MySubscriptionDto(
                s.Id,
                s.RekazSubscriptionId,
                s.Type.ToString(),
                s.Status,
                s.StartDate,
                s.EndDate,
                s.Price,
                s.CreatedAt,
                s.UpdatedAt))
            .ToListAsync(ct);

        _logger.LogInformation(
            "GetMySubscriptions Customer={CustomerId} Status={Status} Page={Page} Count={Count}/{Total}",
            request.CustomerId, request.Status ?? "All", page, items.Count, totalCount);

        return new GetMySubscriptionsResult(items, totalCount, page, pageSize);
    }
}
