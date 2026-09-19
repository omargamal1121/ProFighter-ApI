using Hangfire;
using MediatR;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Application.Subscriptions.Jobs;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Subscriptions.Commands.CreateRekazSubscription;

// Orchestrates: Creates a subscription in Rekaz API.
public class CreateRekazSubscriptionCommandHandler : IRequestHandler<CreateRekazSubscriptionCommand, RekazSubscriptionCreatedResult>
{
    private readonly IRekazClientFactory _clientFactory;
    private readonly ICurrentGymContext _gymContext;
    private readonly IBackgroundJobClient _backgroundJobs;

    public CreateRekazSubscriptionCommandHandler(
        IRekazClientFactory clientFactory,
        ICurrentGymContext gymContext,
        IBackgroundJobClient backgroundJobs)
    {
        _clientFactory = clientFactory;
        _gymContext = gymContext;
        _backgroundJobs = backgroundJobs;
    }

    public async Task<RekazSubscriptionCreatedResult> Handle(CreateRekazSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var gymType = _gymContext.CurrentGymType;
        var rekazClient = _clientFactory.GetClient(gymType);
        var result = await rekazClient.Subscriptions.CreateSubscriptionAsync(request.Request, cancellationToken);

        if (request.Request.CustomerId.HasValue)
        {
            var customerId = request.Request.CustomerId.Value;
            _backgroundJobs.Enqueue<UserSubscriptionSyncJob>(job => job.SyncUserSubscriptionsAsync(customerId, gymType, CancellationToken.None));
        }

        return result;
    }
}

