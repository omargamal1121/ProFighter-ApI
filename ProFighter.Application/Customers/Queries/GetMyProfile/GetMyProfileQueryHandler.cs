using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

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
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
            throw new InvalidOperationException($"Customer with ID {request.CustomerId} not found.");

        var isEmailConfirmed = await _authenticationService
            .IsEmailConfirmedAsync(request.CustomerId, cancellationToken);

        _logger.LogInformation("GetMyProfile fetched for Customer={CustomerId}", request.CustomerId);

        return new GetMyProfileResult(
            Id:                   customer.Id,
            Name:                 customer.Name,
            MobileNumber:         customer.MobileNumber,
            Email:                customer.Email,
            IsEmailConfirmed:     isEmailConfirmed,
            LoyaltyPointsBalance: customer.LoyaltyPointsBalance,
            Source:               customer.Source.ToString(),
            CreatedAt:            customer.CreatedAt,
            UpdatedAt:            customer.UpdatedAt);
    }
}
