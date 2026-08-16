using MediatR;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Subscriptions.Commands.CreateRekazSubscription;

// Orchestrates: Creates a subscription in Rekaz API.
public class CreateRekazSubscriptionCommandHandler : IRequestHandler<CreateRekazSubscriptionCommand, RekazSubscriptionCreatedResult>
{
    private readonly IRekazSubscriptionsClient _rekazSubscriptionsClient;

    public CreateRekazSubscriptionCommandHandler(IRekazSubscriptionsClient rekazSubscriptionsClient)
    {
        _rekazSubscriptionsClient = rekazSubscriptionsClient;
    }

    public async Task<RekazSubscriptionCreatedResult> Handle(CreateRekazSubscriptionCommand request, CancellationToken cancellationToken)
    {
        return await _rekazSubscriptionsClient.CreateSubscriptionAsync(request.Request, cancellationToken);
    }
}
