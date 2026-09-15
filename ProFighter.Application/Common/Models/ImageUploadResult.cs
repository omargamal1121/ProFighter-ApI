namespace ProFighter.Application.Common.Models;

public record ImageUploadResult(
    string Url,
    string PublicId,
    long FileSize,
    string ContentType
);
