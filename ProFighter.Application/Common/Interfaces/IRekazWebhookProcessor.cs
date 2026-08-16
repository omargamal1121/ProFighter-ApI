namespace ProFighter.Application.Common.Interfaces;

public interface IRekazWebhookProcessor
{
    Task ProcessAsync(Guid webhookEventId, CancellationToken ct = default);
}
