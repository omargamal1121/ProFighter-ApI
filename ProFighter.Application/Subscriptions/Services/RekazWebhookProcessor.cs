using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Application.Subscriptions.Services;

public class RekazWebhookProcessor : IRekazWebhookProcessor
{
    private readonly IApplicationDbContext _context;
    private readonly IRekazSubscriptionEventHandler _subscriptionEventHandler;
    private readonly IRekazTransactionEventHandler _transactionEventHandler;
    private readonly ILogger<RekazWebhookProcessor> _logger;

    public RekazWebhookProcessor(
        IApplicationDbContext context,
        IRekazSubscriptionEventHandler subscriptionEventHandler,
        IRekazTransactionEventHandler transactionEventHandler,
        ILogger<RekazWebhookProcessor> logger)
    {
        _context = context;
        _subscriptionEventHandler = subscriptionEventHandler;
        _transactionEventHandler = transactionEventHandler;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid webhookEventId, CancellationToken ct = default)
    {
        var entry = await _context.RekazWebhookInboxEntries.FirstOrDefaultAsync(w => w.Id == webhookEventId, ct);
        if (entry is null || entry.Processed) return; // already handled or somehow missing

        var payload = JsonSerializer.Deserialize<JsonElement>(entry.RawPayload);

        if (entry.EventName.StartsWith("Subscription", StringComparison.Ordinal))
        {
            var dataId = Guid.Parse(payload.GetProperty("Data").GetProperty("Id").GetString()!);
            await _subscriptionEventHandler.HandleAsync(dataId, entry.EventName, ct);
        }
        else if (entry.EventName.StartsWith("Transaction", StringComparison.Ordinal) || entry.EventName.StartsWith("Invoice", StringComparison.Ordinal))
        {
            var dataId = Guid.Parse(payload.GetProperty("Data").GetProperty("Id").GetString()!);
            await _transactionEventHandler.HandleAsync(dataId, entry.EventName, ct);
        }
        else
        {
            _logger.LogInformation("Rekaz webhook {EventName} ({EventId}) recorded, no handler implemented yet.", entry.EventName, webhookEventId);
        }

        entry.MarkProcessed();
        await _context.SaveChangesAsync(ct);
    }
}
