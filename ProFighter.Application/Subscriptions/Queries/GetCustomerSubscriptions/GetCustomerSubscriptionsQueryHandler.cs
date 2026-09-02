using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Queries.GetCustomerSubscriptions;

public class GetCustomerSubscriptionsQueryHandler : IRequestHandler<GetCustomerSubscriptionsQuery, GetCustomerSubscriptionsResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetCustomerSubscriptionsQueryHandler> _logger;

    public GetCustomerSubscriptionsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetCustomerSubscriptionsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetCustomerSubscriptionsResult> Handle(GetCustomerSubscriptionsQuery request, CancellationToken ct)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);

        if (customer is null)
        {
            _logger.LogWarning("Customer {CustomerId} not found", request.CustomerId);
            return new GetCustomerSubscriptionsResult(new List<CustomerSubscriptionDto>(), 0);
        }

        var query = _context.Subscriptions
            .Where(s => s.CustomerId == request.CustomerId);

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
            .ToListAsync(ct);

        var subscriptionDtos = subscriptions.Select(s => new CustomerSubscriptionDto(
            Id: s.Id,
            RekazSubscriptionId: s.RekazSubscriptionId,
            Name: s.Name,
            Type: s.Type,
            Status: s.Status,
            StartDate: s.StartDate,
            EndDate: s.EndDate,
            Price: s.Price,
            IsFullyPaid: true, // TODO: implement payment status tracking based on RemainingAmount from Rekaz
            CreatedAt: s.CreatedAt,
            UpdatedAt: s.UpdatedAt
        )).ToList();

        _logger.LogInformation("Retrieved {Count} subscriptions for customer {CustomerId} with filters applied", 
            subscriptionDtos.Count, request.CustomerId);

        return new GetCustomerSubscriptionsResult(subscriptionDtos, totalCount);
    }
}
