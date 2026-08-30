using MediatR;
using Microsoft.EntityFrameworkCore;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Notifications.Commands.RegisterDeviceToken;

public record RegisterDeviceTokenCommand(Guid UserId, string FcmToken, string? DeviceId, string? Platform) : IRequest<bool>;

public class RegisterDeviceTokenCommandHandler : IRequestHandler<RegisterDeviceTokenCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public RegisterDeviceTokenCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        // 1. Check if the token already exists for this exact user
        var existingToken = await _context.DeviceTokens
            .FirstOrDefaultAsync(t => t.CustomerId == userId && t.FcmToken == request.FcmToken, cancellationToken);

        if (existingToken != null)
        {
            existingToken.UpdateLastUsed();
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        // 2. The token might have been used by another user previously on the same device.
        // It's a good practice to delete/reassign if the same token is registered by a different user.
        var tokenUsedByOthers = await _context.DeviceTokens
            .FirstOrDefaultAsync(t => t.FcmToken == request.FcmToken, cancellationToken);

        if (tokenUsedByOthers != null)
        {
            tokenUsedByOthers.ReassignToCustomer(userId);
        }
        else
        {
            var newToken = new DeviceToken(Guid.NewGuid(), userId, request.FcmToken);
            _context.DeviceTokens.Add(newToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
