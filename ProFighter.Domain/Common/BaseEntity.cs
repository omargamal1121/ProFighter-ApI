namespace ProFighter.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }

    public bool IsDeleted => DeletedAt.HasValue;

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsDeleted()
    {
        if (DeletedAt == null)
        {
            DeletedAt = DateTime.UtcNow;
        }
    }

    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
