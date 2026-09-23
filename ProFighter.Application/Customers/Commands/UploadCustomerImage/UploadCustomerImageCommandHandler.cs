using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Customers.Common;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Customers.Commands.UploadCustomerImage;

public class UploadCustomerImageCommandHandler : IRequestHandler<UploadCustomerImageCommand, Result<CustomerMediaDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IImageService _imageService;
    private readonly ILogger<UploadCustomerImageCommandHandler> _logger;

    public UploadCustomerImageCommandHandler(
        IApplicationDbContext context,
        IImageService imageService,
        ILogger<UploadCustomerImageCommandHandler> logger)
    {
        _context = context;
        _imageService = imageService;
        _logger = logger;
    }

    public async Task<Result<CustomerMediaDto>> Handle(UploadCustomerImageCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer == null)
            return Result<CustomerMediaDto>.Failure($"Customer with ID '{request.CustomerId}' was not found.", 404);

        if (request.Image == null || request.Image.Length == 0)
            return Result<CustomerMediaDto>.Failure("An image file is required.", 400);

        var uploadResult = await _imageService.UploadImageAsync(request.Image, "customers", cancellationToken);
        if (!uploadResult.IsSuccess || uploadResult.Data == null)
            return Result<CustomerMediaDto>.Failure(uploadResult.Message ?? "Failed to upload image.", uploadResult.Status);

        var media = new Media(
            cloudinaryUrl: uploadResult.Data.Url,
            cloudinaryPublicId: uploadResult.Data.PublicId,
            type: MediaType.Image,
            ownerType: MediaOwnerType.Customer,
            ownerId: customer.Id,
            purpose: request.Purpose,
            displayOrder: request.DisplayOrder
        );

        _context.Medias.Add(media);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded image {MediaId} for Customer {CustomerId}", media.Id, customer.Id);
        return Result<CustomerMediaDto>.Success(CustomerMediaDto.FromEntity(media), "Image uploaded successfully.", 201);
    }
}
