using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class Trainer : BaseEntity
{
    private readonly List<Media> _medias = new();

    public string Name { get; private set; }
    public string? Bio { get; private set; }
    public SubscriptionType Specialization { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<Media> Medias => _medias.AsReadOnly();

    // EF Core Constructor
    private Trainer() : base()
    {
        Name = null!;
    }

    public Trainer(Guid id, string name, SubscriptionType specialization, string? bio = null, bool isActive = true) : base()
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Trainer name cannot be empty.", nameof(name));

        Id = id;
        Name = name;
        Specialization = specialization;
        Bio = bio;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name, SubscriptionType specialization, string? bio)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Trainer name cannot be empty.", nameof(name));

        Name = name;
        Specialization = specialization;
        Bio = bio;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }
}
