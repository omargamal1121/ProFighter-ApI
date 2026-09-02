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
        if (entry is null || entry.Processed) return; 

        var payload = JsonSerializer.Deserialize<JsonElement>(entry.RawPayload);

		if (entry.EventName.StartsWith("Subscription", StringComparison.Ordinal))
		{
			var dataId = ExtractDataId(payload);
			await _subscriptionEventHandler.HandleAsync(dataId, entry.EventName, ct);
		}
		else if (entry.EventName.StartsWith("Transaction", StringComparison.Ordinal) || entry.EventName.StartsWith("Invoice", StringComparison.Ordinal))
		{
			var dataId = ExtractDataId(payload);
			await _transactionEventHandler.HandleAsync(dataId, entry.EventName, ct);
		}
		else
        {
            _logger.LogInformation("Rekaz webhook {EventName} ({EventId}) recorded, no handler implemented yet.", entry.EventName, webhookEventId);
        }

        entry.MarkProcessed();
        await _context.SaveChangesAsync(ct);
    }
	private static bool TryGetPropertyCI(JsonElement element, string propertyName, out JsonElement value)
	{
		foreach (var property in element.EnumerateObject())
		{
			if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
			{
				value = property.Value;
				return true;
			}
		}
		value = default;
		return false;
	}

	private static Guid ExtractDataId(JsonElement payload)
	{
		if (!TryGetPropertyCI(payload, "Data", out var data))
			throw new InvalidOperationException("Webhook payload missing 'data' property.");
		if (!TryGetPropertyCI(data, "Id", out var id))
			throw new InvalidOperationException("Webhook payload missing 'data.id' property.");
		return Guid.Parse(id.GetString()!);
	}
}
