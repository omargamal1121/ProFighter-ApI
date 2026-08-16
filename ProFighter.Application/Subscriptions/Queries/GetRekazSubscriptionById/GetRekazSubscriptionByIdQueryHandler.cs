using MediatR;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Subscriptions.Queries.GetRekazSubscriptionById;

// Orchestrates: Fetches a single subscription by ID from Rekaz API.
public class GetRekazSubscriptionByIdQueryHandler : IRequestHandler<GetRekazSubscriptionByIdQuery, RekazSubscriptionResult?>
{
    private readonly IRekazSubscriptionsClient _rekazSubscriptionsClient;

    public GetRekazSubscriptionByIdQueryHandler(IRekazSubscriptionsClient rekazSubscriptionsClient)
    {
        _rekazSubscriptionsClient = rekazSubscriptionsClient;
    }

    public async Task<RekazSubscriptionResult?> Handle(GetRekazSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        return await _rekazSubscriptionsClient.GetSubscriptionByIdAsync(request.Id, cancellationToken);
    }
}
