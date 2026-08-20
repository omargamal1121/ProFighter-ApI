using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Subscriptions.Common;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Queries.GetSubscriptions;

public class GetSubscriptionsQueryHandler : IRequestHandler<GetSubscriptionsQuery, GetSubscriptionsResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetSubscriptionsQueryHandler> _logger;

    public GetSubscriptionsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetSubscriptionsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetSubscriptionsResult> Handle(GetSubscriptionsQuery request, CancellationToken ct)
    {
        var query = _context.Subscriptions.AsQueryable();

        // Apply customer filter if provided
        if (request.CustomerId.HasValue)
        {
            query = query.Where(s => s.CustomerId == request.CustomerId.Value);
        }

        // Apply filters
        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(s => s.Status == request.Status);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(s => s.Type == request.Type.Value);
        }

        if (request.StartDateFrom.HasValue)
        {
            query = query.Where(s => s.StartDate >= request.StartDateFrom.Value);
        }

        if (request.StartDateTo.HasValue)
        {
            query = query.Where(s => s.StartDate <= request.StartDateTo.Value);
        }

        if (request.EndDateFrom.HasValue)
        {
            query = query.Where(s => s.EndDate.HasValue && s.EndDate >= request.EndDateFrom.Value);
        }

        if (request.EndDateTo.HasValue)
        {
            query = query.Where(s => s.EndDate.HasValue && s.EndDate <= request.EndDateTo.Value);
        }

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
            {
                query = query.Where(s => s.Status == "Active" && 
                    (!s.EndDate.HasValue || s.EndDate >= DateTime.UtcNow));
            }
            else
            {
                query = query.Where(s => s.Status != "Active" || 
                    (s.EndDate.HasValue && s.EndDate < DateTime.UtcNow));
            }
        }

        var totalCount = await query.CountAsync(ct);

        var subscriptions = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip(request.SkipCount)
            .Take(Math.Min(request.MaxResultCount, 100)) // Cap at 100 to prevent excessive loads
            .ToListAsync(ct);

        var subscriptionDtos = subscriptions.Select(s => new SubscriptionDto(
            Id: s.Id,
            CustomerId: s.CustomerId,
            RekazSubscriptionId: s.RekazSubscriptionId,
            Type: s.Type.ToString(),
            Status: s.Status,
            StartDate: s.StartDate,
            EndDate: s.EndDate,
            Price: s.Price,
            IsFullyPaid: true, // TODO: implement payment status tracking based on RemainingAmount from Rekaz
            CreatedAt: s.CreatedAt,
            UpdatedAt: s.UpdatedAt
        )).ToList();

        _logger.LogInformation("Retrieved {Count} subscriptions from local database (Total: {Total})", 
            subscriptionDtos.Count, totalCount);

        return new GetSubscriptionsResult(subscriptionDtos, totalCount);
    }
}
