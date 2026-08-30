using MediatR;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Interfaces.Auth;
using ProFighter.Application.Common.Models.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Auth.Commands.CompleteAccount;

// Orchestrates: Password reset, email update, completion marking in a transaction, followed by token generation.
public sealed class CompleteAccountCommandHandler : IRequestHandler<CompleteAccountCommand, CompleteAccountResult>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CompleteAccountCommandHandler> _logger;

    public CompleteAccountCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IUnitOfWork unitOfWork,
        IApplicationDbContext context,
        ILogger<CompleteAccountCommandHandler> logger)
    {
        _identityService     = identityService;
        _tokenService        = tokenService;
        _refreshTokenService = refreshTokenService;
        _unitOfWork         = unitOfWork;
        _context            = context;
        _logger             = logger;
    }

    public async Task<CompleteAccountResult> Handle(CompleteAccountCommand request, CancellationToken cancellationToken)
    {
        // Validate email uniqueness before starting the transaction
        var isUnique = await _identityService.IsEmailUniqueAsync(request.Email, request.UserId, cancellationToken);
        if (!isUnique)
            throw new InvalidOperationException($"The email address '{request.Email}' is already in use by another account.");

        // Execute the identity updates inside a transaction
        var newSecurityStamp = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // 1. Reset password
            await _identityService.ResetPasswordAsync(request.UserId, request.NewPassword, ct);

            // 2. Set email
            await _identityService.SetEmailAsync(request.UserId, request.Email, ct);

            // 3. Mark account completed (updates user in DB and returns the new SecurityStamp)
            var securityStamp = await _identityService.MarkAccountCompletedAsync(request.UserId, ct);

            // No need to explicitly call SaveChanges for Identity operations since UserManager does it,
            // but the transaction ensures they all succeed or fail together.

            return securityStamp;
        }, cancellationToken);

        // Retrieve updated user data to get roles/claims for the token
        var identity = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);

        // Issue normal tokens
        var accessToken = await _tokenService.GenerateTokenAsync(
            new TokenGenerationRequest(identity.UserId, identity.Roles, identity.Claims));

        var refreshToken = await _refreshTokenService.GenerateAndStoreAsync(
            identity.UserId,
            newSecurityStamp,
            reuseExisting: false);

        // Save the newly generated refresh token
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Account completed and tokens issued for UserId: {UserId}", request.UserId);

        return new CompleteAccountResult(Token: accessToken, RefreshToken: refreshToken);
    }
}
