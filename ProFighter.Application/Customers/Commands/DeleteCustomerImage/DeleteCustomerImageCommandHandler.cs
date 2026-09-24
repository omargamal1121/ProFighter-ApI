using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Application.Customers.Commands.DeleteCustomerImage;

public class DeleteCustomerImageCommandHandler : IRequestHandler<DeleteCustomerImageCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DeleteCustomerImageCommandHandler> _logger;

    public DeleteCustomerImageCommandHandler(
        IApplicationDbContext context,
        ILogger<DeleteCustomerImageCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteCustomerImageCommand request, CancellationToken cancellationToken)
    {
        var media = await _context.Medias
            .FirstOrDefaultAsync(m => m.Id == request.ImageId && m.CustomerId == request.CustomerId, cancellationToken);

        if (media == null)
            return Result<bool>.Failure($"Image with ID '{request.ImageId}' was not found for this customer.", 404);

        // Soft-delete the media entry (keeps DB FK intact to satisfy CK_Media_SingleOwner)
        media.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft-deleted image {ImageId} for Customer {CustomerId}", request.ImageId, request.CustomerId);
        return Result<bool>.Success(true, "Customer image deleted successfully.");
    }
}
