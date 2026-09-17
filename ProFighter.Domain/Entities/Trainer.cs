using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class Trainer : BaseEntity
{
    private readonly List<Media> _medias = new();

    public string Name { get; private set; }
    public string? Bio { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<Media> Medias => _medias.AsReadOnly();
    public GymType GymType { get; private set; } = GymType.ProFighter;

    // EF Core Constructor
    private Trainer() : base()
    {
        Name = null!;
    }

    public Trainer(Guid id, string name, string? bio = null, bool isActive = true, GymType gymType = GymType.ProFighter) : base()
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Trainer name cannot be empty.", nameof(name));

        Id = id;
        Name = name;
        Bio = bio;
        IsActive = isActive;
        GymType = gymType;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name, string? bio, GymType? gymType = null, bool? isActive = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Trainer name cannot be empty.", nameof(name));

        Name = name;
        Bio = bio;
        if (gymType.HasValue) GymType = gymType.Value;
        if (isActive.HasValue) IsActive = isActive.Value;
        MarkAsUpdated();
    }

    public void AddMedia(Media media)
    {
        _medias.Add(media);
        MarkAsUpdated();
    }

    public void RemoveMedia(Media media)
    {
        _medias.Remove(media);
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
