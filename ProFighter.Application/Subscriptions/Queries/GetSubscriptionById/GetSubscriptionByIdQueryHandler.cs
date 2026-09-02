using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Subscriptions.Common;
using ProFighter.Domain.Entities;

namespace ProFighter.Application.Subscriptions.Queries.GetSubscriptionById;

public class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, GetSubscriptionByIdResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetSubscriptionByIdQueryHandler> _logger;

    public GetSubscriptionByIdQueryHandler(
        IApplicationDbContext context,
        ILogger<GetSubscriptionByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetSubscriptionByIdResult> Handle(GetSubscriptionByIdQuery request, CancellationToken ct)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.RekazSubscriptionId == request.RekazSubscriptionId, ct);

        if (subscription is null)
        {
            _logger.LogWarning("Subscription with Rekaz ID {RekazSubscriptionId} not found", request.RekazSubscriptionId);
            return new GetSubscriptionByIdResult(null);
        }

        var subscriptionDto = new SubscriptionDto(
            Id: subscription.Id,
            CustomerId: subscription.CustomerId,
            RekazSubscriptionId: subscription.RekazSubscriptionId,
            Name: subscription.Name,
            Type: subscription.Type.ToString(),
            Status: subscription.Status,
            StartDate: subscription.StartDate,
            EndDate: subscription.EndDate,
            Price: subscription.Price,
            IsFullyPaid: true, // TODO: implement payment status tracking based on RemainingAmount from Rekaz
            CreatedAt: subscription.CreatedAt,
            UpdatedAt: subscription.UpdatedAt
        );

        _logger.LogInformation("Retrieved subscription with Rekaz ID {RekazSubscriptionId}", request.RekazSubscriptionId);

        return new GetSubscriptionByIdResult(subscriptionDto);
    }
}
