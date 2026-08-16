using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace ProFighter.Application.Common.Models.Auth;

public class TokenGenerationRequest
{
    public Guid UserId { get; }
    public IReadOnlyCollection<string> Roles { get; }
    public IReadOnlyCollection<Claim>? Claims { get; }

    public TokenGenerationRequest(Guid userId, IReadOnlyCollection<string> roles, IReadOnlyCollection<Claim>? claims = null)
    {
        UserId = userId;
        Roles = roles;
        Claims = claims;
    }
}
