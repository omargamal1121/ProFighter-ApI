using System;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Customers.Common;

public record CustomerMediaDto(
    Guid Id,
    string CloudinaryUrl,
    string CloudinaryPublicId,
    MediaType Type,
    MediaPurpose Purpose,
    int DisplayOrder,
    DateTime CreatedAt
)
{
    public static CustomerMediaDto FromEntity(Media media)
    {
        return new CustomerMediaDto(
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
