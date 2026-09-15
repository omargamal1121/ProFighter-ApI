using Microsoft.AspNetCore.Http;
using ProFighter.Application.Common.Models;

namespace ProFighter.Application.Common.Interfaces;

public interface IImageService
{
    Task<Result<ImageUploadResult>> UploadImageAsync(
        IFormFile file,
        string folderName,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default);

    bool IsValidFile(IFormFile file, out string? errorMessage);
}
