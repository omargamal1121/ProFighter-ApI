namespace ProFighter.Domain.Entities;

public class RekazWebhookInboxEntry
{
    public Guid Id { get; private set; }
    public string EventName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string RawPayload { get; private set; }
    public bool Processed { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    // EF Core Constructor
    private RekazWebhookInboxEntry()
    {
        EventName = null!;
        RawPayload = null!;
    }

    public RekazWebhookInboxEntry(Guid id, string eventName, string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name cannot be empty.", nameof(eventName));
        if (string.IsNullOrWhiteSpace(rawPayload))
            throw new ArgumentException("Raw payload cannot be empty.", nameof(rawPayload));

        Id = id;
        EventName = eventName;
        RawPayload = rawPayload;
        CreatedAt = DateTime.UtcNow;
        Processed = false;
        ProcessedAt = null;
    }

    public void MarkAsProcessed()
    {
        Processed = true;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkProcessed()
    {
        Processed = true;
        ProcessedAt = DateTime.UtcNow;
    }
}
