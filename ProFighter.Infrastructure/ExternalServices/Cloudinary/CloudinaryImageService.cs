using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ImageUploadResult = ProFighter.Application.Common.Models.ImageUploadResult;

namespace ProFighter.Infrastructure.ExternalServices.Cloudinary;

public class CloudinaryImageService : IImageService
{
    private readonly CloudinaryDotNet.Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryImageService> _logger;
    private readonly IConfiguration _configuration;

    private int MaxFileSize => _configuration.GetValue<int>("Security:FileUpload:MaxFileSizeMB", 5) * 1024 * 1024;
    private string[] AllowedContentTypes => _configuration.GetSection("Security:FileUpload:AllowedContentTypes").Get<string[]>()
        ?? new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
    private string[] AllowedExtensions => _configuration.GetSection("Security:FileUpload:AllowedExtensions").Get<string[]>()
        ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly byte[][] _fileSignatures = {
        new byte[] { 0xFF, 0xD8, 0xFF },       // JPEG
        new byte[] { 0x89, 0x50, 0x4E, 0x47 },  // PNG
        new byte[] { 0x47, 0x49, 0x46, 0x38 },  // GIF
        new byte[] { 0x52, 0x49, 0x46, 0x46 },  // WEBP
    };

    public CloudinaryImageService(
        CloudinaryDotNet.Cloudinary cloudinary,
        ILogger<CloudinaryImageService> logger,
        IConfiguration configuration)
    {
        _cloudinary = cloudinary;
        _logger = logger;
        _configuration = configuration;
    }

    public bool IsValidFile(IFormFile file, out string? errorMessage)
    {
        errorMessage = null;

        if (file == null || file.Length == 0)
        {
            errorMessage = "Image file is null or empty.";
            return false;
        }

        if (file.Length > MaxFileSize)
        {
            errorMessage = $"File size exceeds the maximum limit of {MaxFileSize / (1024 * 1024)}MB.";
            return false;
        }

        if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
        {
            errorMessage = $"Invalid content type: {file.ContentType}. Allowed: {string.Join(", ", AllowedContentTypes)}";
            return false;
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension.ToLower()))
        {
            errorMessage = $"Invalid file extension '{extension}'. Allowed: {string.Join(", ", AllowedExtensions)}";
            return false;
        }

        try
        {
            using var stream = file.OpenReadStream();
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
            var headerBytes = reader.ReadBytes(8);
            if (!_fileSignatures.Any(sig => headerBytes.Take(sig.Length).SequenceEqual(sig)))
            {
                errorMessage = "Invalid image file: header signature does not match any supported format.";
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate file signature for {FileName}", file.FileName);
            errorMessage = "Could not validate file structure.";
            return false;
        }

        return true;
    }

    public async Task<Result<ImageUploadResult>> UploadImageAsync(
        IFormFile file,
        string folderName,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidFile(file, out var validationError))
            return Result<ImageUploadResult>.Failure(validationError ?? "Invalid image file.", 400);

        try
        {
            await using var stream = file.OpenReadStream();
            var publicId = Guid.NewGuid().ToString();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName,
                PublicId = publicId,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult?.SecureUrl == null || uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogError("Cloudinary upload failed: {Error}", uploadResult?.Error?.Message);
                return Result<ImageUploadResult>.Failure($"Cloudinary upload failed: {uploadResult?.Error?.Message}", 500);
            }

            _logger.LogInformation("Uploaded image {PublicId} to folder {Folder}", uploadResult.PublicId, folderName);

            return Result<ImageUploadResult>.Success(new ImageUploadResult(
                Url: uploadResult.SecureUrl.ToString(),
                PublicId: uploadResult.PublicId,
                FileSize: file.Length,
                ContentType: file.ContentType
            ), "Image uploaded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to Cloudinary folder {Folder}", folderName);
            return Result<ImageUploadResult>.Failure($"Failed to upload image: {ex.Message}", 500);
        }
    }

    public async Task<Result<bool>> DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return Result<bool>.Failure("Public ID cannot be empty.", 400);

        try
        {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Result == "ok" || result.Result == "not found")
            {
                _logger.LogInformation("Deleted image from Cloudinary: {PublicId}", publicId);
                return Result<bool>.Success(true, "Image deleted from Cloudinary.");
            }

            _logger.LogWarning("Cloudinary returned '{Result}' for {PublicId}", result.Result, publicId);
            return Result<bool>.Failure($"Cloudinary deletion returned: {result.Result}", 500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {PublicId} from Cloudinary", publicId);
            return Result<bool>.Failure($"Failed to delete image: {ex.Message}", 500);
        }
    }
}
