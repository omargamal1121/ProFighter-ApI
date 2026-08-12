using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class NotificationLog : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string Title { get; private set; }
    public string Body { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationStatus Status { get; private set; }

    // EF Core Constructor
    private NotificationLog() : base()
    {
        Title = null!;
        Body = null!;
    }

    public NotificationLog(
        Guid id,
        Guid customerId,
        string title,
        string body,
        NotificationChannel channel,
        NotificationStatus status) : base()
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body cannot be empty.", nameof(body));

        Id = id;
        CustomerId = customerId;
        Title = title;
        Body = body;
        Channel = channel;
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(NotificationStatus status)
    {
        Status = status;
        MarkAsUpdated();
    }
}
