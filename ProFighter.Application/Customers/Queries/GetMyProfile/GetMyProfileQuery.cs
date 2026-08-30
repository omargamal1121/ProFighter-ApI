using MediatR;

namespace ProFighter.Application.Customers.Queries.GetMyProfile;

/// <summary>
/// Returns the authenticated customer's profile.
/// CustomerId is resolved server-side from the JWT — never supplied by the client.
/// </summary>
public record GetMyProfileQuery(
    /// <summary>Resolved from JWT (Customer.Id == IdentityUser.Id).</summary>
    Guid CustomerId) : IRequest<GetMyProfileResult>;

public record GetMyProfileResult(
    Guid Id,
    string Name,
    string MobileNumber,
    string? Email,
    bool IsEmailConfirmed,
    int LoyaltyPointsBalance,
    string Source,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
