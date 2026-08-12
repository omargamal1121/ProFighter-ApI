using ProFighter.Domain.Common;

namespace ProFighter.Domain.Entities;

public class Gym : BaseEntity
{
    private readonly List<Media> _medias = new();

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Address { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public IReadOnlyCollection<Media> Medias => _medias.AsReadOnly();

    // EF Core Constructor
    private Gym() : base()
    {
        Name = null!;
    }

    public Gym(Guid id, string name, string? description = null, string? address = null, string? phoneNumber = null, string? email = null) : base()
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Gym name cannot be empty.", nameof(name));

        Id = id;
        Name = name;
        Description = description;
        Address = address;
        PhoneNumber = phoneNumber;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string? description, string? address, string? phoneNumber, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Gym name cannot be empty.", nameof(name));

        Name = name;
        Description = description;
        Address = address;
        PhoneNumber = phoneNumber;
        Email = email;
        MarkAsUpdated();
    }
}
