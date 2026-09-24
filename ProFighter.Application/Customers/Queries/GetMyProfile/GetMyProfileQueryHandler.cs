using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Customers.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler
    : IRequestHandler<GetMyProfileQuery, GetMyProfileResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<GetMyProfileQueryHandler> _logger;

    public GetMyProfileQueryHandler(
        IApplicationDbContext context,
        IAuthenticationService authenticationService,
        ILogger<GetMyProfileQueryHandler> logger)
    {
        _context                 = context;
        _authenticationService   = authenticationService;
        _logger                  = logger;
    }

    public async Task<GetMyProfileResult> Handle(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == request.CustomerId)
            .Select(c => new
            {
                Customer = c,
                ImageUrl = _context.Medias
                    .Where(m => m.CustomerId == c.Id
                             && m.Purpose == MediaPurpose.ProfileImage)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.CloudinaryUrl)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
            throw new InvalidOperationException($"Customer with ID {request.CustomerId} not found.");

        var customer = result.Customer;
        var imageUrl = result.ImageUrl;

        var isEmailConfirmed = await _authenticationService
            .IsEmailConfirmedAsync(request.CustomerId, cancellationToken);

        _logger.LogInformation("GetMyProfile fetched for Customer={CustomerId}", request.CustomerId);

        return new GetMyProfileResult(
            Id:                   customer.Id,
            Name:                 customer.Name,
            MobileNumber:         customer.MobileNumber,
            Email:                customer.Email,
            ImageUrl:             imageUrl,
            IsEmailConfirmed:     isEmailConfirmed,
            LoyaltyPointsBalance: customer.LoyaltyPointsBalance,
            Source:               customer.Source.ToString(),
            CreatedAt:            customer.CreatedAt,
            UpdatedAt:            customer.UpdatedAt);
    }
}
