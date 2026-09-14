using Microsoft.AspNetCore.Http;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Enums;
using System.Security.Claims;

namespace ProFighter.API.Services;

public class CurrentGymContext : ICurrentGymContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentGymContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public GymType CurrentGymType
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var claimValue = user.FindFirstValue("GymType");
                if (!string.IsNullOrEmpty(claimValue) && int.TryParse(claimValue, out int gymTypeInt))
                {
                    return (GymType)gymTypeInt;
                }
            }

            // Fallback for unauthenticated requests, check header
            if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("X-Gym-Type", out var headerValue) == true)
            {
                if (!string.IsNullOrEmpty(headerValue) && int.TryParse(headerValue, out int headerGymTypeInt))
                {
                    return (GymType)headerGymTypeInt;
                }
            }

            // Default
            return GymType.ProFighter;
        }
    }
}
