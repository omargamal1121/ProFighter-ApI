using MediatR;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Subscriptions.Queries.GetRekazSubscriptionById;

// Orchestrates: Fetches a single subscription by ID from Rekaz API.
public class GetRekazSubscriptionByIdQueryHandler : IRequestHandler<GetRekazSubscriptionByIdQuery, RekazSubscriptionResult?>
{
    private readonly IRekazClientFactory _clientFactory;
    private readonly ICurrentGymContext _gymContext;

    public GetRekazSubscriptionByIdQueryHandler(
        IRekazClientFactory clientFactory,
        ICurrentGymContext gymContext)
    {
        _clientFactory = clientFactory;
        _gymContext = gymContext;
    }

    public async Task<RekazSubscriptionResult?> Handle(GetRekazSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        var rekazClient = _clientFactory.GetClient(_gymContext.CurrentGymType);
        return await rekazClient.Subscriptions.GetSubscriptionByIdAsync(request.Id, cancellationToken);
    }
}
