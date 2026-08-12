using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class Media : BaseEntity
{
    public string CloudinaryUrl { get; private set; }
    public string CloudinaryPublicId { get; private set; }
    public MediaType Type { get; private set; }
    public MediaOwnerType OwnerType { get; private set; }
    public Guid OwnerId { get; private set; }
    public MediaPurpose Purpose { get; private set; }
    public int DisplayOrder { get; private set; }

    // EF Core Constructor
    private Media() : base()
    {
        CloudinaryUrl = null!;
        CloudinaryPublicId = null!;
    }

    public Media(
        Guid id,
        string cloudinaryUrl,
        string cloudinaryPublicId,
        MediaType type,
        MediaOwnerType ownerType,
        Guid ownerId,
        MediaPurpose purpose,
        int displayOrder = 0) : base()
    {
        if (string.IsNullOrWhiteSpace(cloudinaryUrl))
            throw new ArgumentException("Cloudinary URL cannot be empty.", nameof(cloudinaryUrl));
        if (string.IsNullOrWhiteSpace(cloudinaryPublicId))
            throw new ArgumentException("Cloudinary Public ID cannot be empty.", nameof(cloudinaryPublicId));

        Id = id;
        CloudinaryUrl = cloudinaryUrl;
        CloudinaryPublicId = cloudinaryPublicId;
        Type = type;
        OwnerType = ownerType;
        OwnerId = ownerId;
        Purpose = purpose;
        DisplayOrder = displayOrder;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        MarkAsUpdated();
    }
}
