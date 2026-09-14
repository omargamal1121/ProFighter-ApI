using MediatR;
using Microsoft.Extensions.Logging;

namespace ProFighter.Application.Customers.Jobs;

public class NightlyCustomerSyncJob
{
    private readonly ISender _mediator;
    private readonly ILogger<NightlyCustomerSyncJob> _logger;

    public NightlyCustomerSyncJob(ISender mediator, ILogger<NightlyCustomerSyncJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("NightlyCustomerSyncJob starting...");
        var result = await _mediator.Send(new Commands.SyncCustomers.SyncCustomersCommand(), cancellationToken);
        _logger.LogInformation("NightlyCustomerSyncJob completed successfully. Total: {Total}, Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Errors: {Errors}",
            result.TotalProcessed, result.Created, result.Updated, result.Skipped, result.Errors);
    }
}
