using MediatR;
using Microsoft.Extensions.Logging;

namespace ProFighter.Application.Subscriptions.Jobs;

public class NightlySubscriptionSyncJob
{
    private readonly ISender _mediator;
    private readonly ILogger<NightlySubscriptionSyncJob> _logger;

    public NightlySubscriptionSyncJob(ISender mediator, ILogger<NightlySubscriptionSyncJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("NightlySubscriptionSyncJob starting...");
        var result = await _mediator.Send(new Commands.SyncSubscriptions.SyncSubscriptionsCommand(), cancellationToken);
        _logger.LogInformation("NightlySubscriptionSyncJob completed successfully. Total: {Total}, Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Errors: {Errors}",
            result.TotalProcessed, result.Created, result.Updated, result.Skipped, result.Errors);
    }
}
