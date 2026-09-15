using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Trainers.Common;

public record TrainerMediaDto(
    Guid Id,
    string CloudinaryUrl,
    string CloudinaryPublicId,
    MediaType Type,
    MediaPurpose Purpose,
    int DisplayOrder,
    DateTime CreatedAt
)
{
    public static TrainerMediaDto FromEntity(Media media)
    {
        return new TrainerMediaDto(
            media.Id,
            media.CloudinaryUrl,
            media.CloudinaryPublicId,
            media.Type,
            media.Purpose,
            media.DisplayOrder,
            media.CreatedAt
        );
    }
}
