using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class Media : BaseEntity
{
    public string CloudinaryUrl { get; private set; }
    public string CloudinaryPublicId { get; private set; }
    public MediaType Type { get; private set; }
    public MediaPurpose Purpose { get; private set; }
    public int DisplayOrder { get; private set; }

    // Exclusive-arc foreign keys — exactly one must be non-null (enforced by DB CHECK constraint)
    public Guid? CustomerId { get; private set; }
    public Guid? TrainerId { get; private set; }
    public Guid? GymId { get; private set; }
    public Guid? ProductId { get; private set; }

    // Navigation properties (EF Core populated; no EF/Cloudinary types here per CA boundary rules)
    public Customer? Customer { get; private set; }
    public Trainer? Trainer { get; private set; }
    public Gym? Gym { get; private set; }
    public Product? Product { get; private set; }

    // Required by EF Core
    private Media() : base()
    {
        CloudinaryUrl = null!;
        CloudinaryPublicId = null!;
    }

    // ── Static factory methods ──────────────────────────────────────────────

    public static Media ForCustomer(
        Guid customerId,
        string cloudinaryUrl,
        string cloudinaryPublicId,
        MediaType type,
        MediaPurpose purpose,
        int displayOrder = 0)
    {
        ValidateCloudinary(cloudinaryUrl, cloudinaryPublicId);
        return new Media
        {
            CloudinaryUrl = cloudinaryUrl,
            CloudinaryPublicId = cloudinaryPublicId,
            Type = type,
            Purpose = purpose,
            DisplayOrder = displayOrder,
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static Media ForTrainer(
        Guid trainerId,
        string cloudinaryUrl,
        string cloudinaryPublicId,
        MediaType type,
        MediaPurpose purpose,
        int displayOrder = 0)
    {
        ValidateCloudinary(cloudinaryUrl, cloudinaryPublicId);
        return new Media
        {
            CloudinaryUrl = cloudinaryUrl,
            CloudinaryPublicId = cloudinaryPublicId,
            Type = type,
            Purpose = purpose,
            DisplayOrder = displayOrder,
            TrainerId = trainerId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static Media ForGym(
        Guid gymId,
        string cloudinaryUrl,
        string cloudinaryPublicId,
        MediaType type,
        MediaPurpose purpose,
        int displayOrder = 0)
    {
        ValidateCloudinary(cloudinaryUrl, cloudinaryPublicId);
        return new Media
        {
            CloudinaryUrl = cloudinaryUrl,
            CloudinaryPublicId = cloudinaryPublicId,
            Type = type,
            Purpose = purpose,
            DisplayOrder = displayOrder,
            GymId = gymId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static Media ForProduct(
        Guid productId,
        string cloudinaryUrl,
        string cloudinaryPublicId,
        MediaType type,
        MediaPurpose purpose,
        int displayOrder = 0)
    {
        ValidateCloudinary(cloudinaryUrl, cloudinaryPublicId);
        return new Media
        {
            CloudinaryUrl = cloudinaryUrl,
            CloudinaryPublicId = cloudinaryPublicId,
            Type = type,
            Purpose = purpose,
            DisplayOrder = displayOrder,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ── Behaviour ───────────────────────────────────────────────────────────

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        MarkAsUpdated();
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private static void ValidateCloudinary(string url, string publicId)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Cloudinary URL cannot be empty.", nameof(url));
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException("Cloudinary Public ID cannot be empty.", nameof(publicId));
    }
}
