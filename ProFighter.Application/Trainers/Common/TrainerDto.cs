using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Trainers.Common;

public record TrainerDto(
    Guid Id,
    string Name,
    SubscriptionType Specialization,
    string? Bio,
    bool IsActive,
    GymType GymType,
    string? PrimaryImageUrl,
    List<TrainerMediaDto> Medias,
    DateTime CreatedAt,
    DateTime? UpdatedAt
)
{
    public static TrainerDto FromEntity(Trainer trainer)
    {
        var medias = trainer.Medias.Select(TrainerMediaDto.FromEntity).ToList();
        var primaryImage = trainer.Medias
            .Where(m => m.Purpose == MediaPurpose.ProfileImage)
            .OrderBy(m => m.DisplayOrder)
            .FirstOrDefault()?.CloudinaryUrl ?? trainer.Medias.FirstOrDefault()?.CloudinaryUrl;

        return new TrainerDto(
            trainer.Id,
            trainer.Name,
            trainer.Specialization,
            trainer.Bio,
            trainer.IsActive,
            trainer.GymType,
            primaryImage,
            medias,
            trainer.CreatedAt,
            trainer.UpdatedAt
        );
    }
}
