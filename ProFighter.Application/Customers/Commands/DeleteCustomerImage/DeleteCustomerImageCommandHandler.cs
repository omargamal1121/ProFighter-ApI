using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Customers.Commands.DeleteCustomerImage;

public class DeleteCustomerImageCommandHandler : IRequestHandler<DeleteCustomerImageCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IImageService _imageService;
    private readonly ILogger<DeleteCustomerImageCommandHandler> _logger;

    public DeleteCustomerImageCommandHandler(
        IApplicationDbContext context,
        IImageService imageService,
        ILogger<DeleteCustomerImageCommandHandler> logger)
    {
        _context = context;
        _imageService = imageService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteCustomerImageCommand request, CancellationToken cancellationToken)
    {
        var media = await _context.Medias
            .FirstOrDefaultAsync(m => m.Id == request.ImageId && m.OwnerId == request.CustomerId && m.OwnerType == MediaOwnerType.Customer, cancellationToken);

        if (media == null)
            return Result<bool>.Failure($"Image with ID '{request.ImageId}' was not found for this customer.", 404);

        if (!string.IsNullOrEmpty(media.CloudinaryPublicId))
        {
            await _imageService.DeleteImageAsync(media.CloudinaryPublicId, cancellationToken);
        }

        _context.Medias.Remove(media);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted image {ImageId} for Customer {CustomerId}", request.ImageId, request.CustomerId);
        return Result<bool>.Success(true, "Customer image deleted successfully.");
    }
}
