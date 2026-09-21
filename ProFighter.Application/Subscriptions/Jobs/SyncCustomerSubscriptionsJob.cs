using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Jobs;

[Obsolete("Per-customer subscription sync has been removed in favor of webhook + nightly full sync.")]
public class SyncCustomerSubscriptionsJob
{
    private readonly ILogger<SyncCustomerSubscriptionsJob> _logger;

    public SyncCustomerSubscriptionsJob(ILogger<SyncCustomerSubscriptionsJob> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(
        GymType gymType,
        Guid rekazCustomerId,
        DateTimeOffset transactionCreatedAtUtc,
        List<Guid> productIds,
        PerformContext? performContext = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("SyncCustomerSubscriptionsJob is obsolete no-op. Skipping execution for {GymType}, RekazCustomerId={RekazCustomerId}.", gymType, rekazCustomerId);
        return Task.CompletedTask;
    }
}
