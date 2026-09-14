using MediatR;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Subscriptions.Commands.CreateRekazSubscription;

// Orchestrates: Creates a subscription in Rekaz API.
public class CreateRekazSubscriptionCommandHandler : IRequestHandler<CreateRekazSubscriptionCommand, RekazSubscriptionCreatedResult>
{
    private readonly IRekazClientFactory _clientFactory;
    private readonly ICurrentGymContext _gymContext;

    public CreateRekazSubscriptionCommandHandler(
        IRekazClientFactory clientFactory,
        ICurrentGymContext gymContext)
    {
        _clientFactory = clientFactory;
        _gymContext = gymContext;
    }

    public async Task<RekazSubscriptionCreatedResult> Handle(CreateRekazSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var rekazClient = _clientFactory.GetClient(_gymContext.CurrentGymType);
        return await rekazClient.Subscriptions.CreateSubscriptionAsync(request.Request, cancellationToken);
    }
}
