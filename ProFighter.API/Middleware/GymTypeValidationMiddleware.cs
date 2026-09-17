using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProFighter.API.Middleware;

public class GymTypeValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GymTypeValidationMiddleware> _logger;

    public GymTypeValidationMiddleware(RequestDelegate next, ILogger<GymTypeValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/hangfire") ||
            context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/webhooks") ||
            context.Request.Path.StartsWithSegments("/api/auth/admin") ||
            context.Request.Path.StartsWithSegments("/api/admin") ||
            context.User.IsInRole("Admin"))
        {
            await _next(context);
            return;
        }

        var headerExists = context.Request.Headers.TryGetValue("X-Gym-Type", out var headerValue);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            // ── Authenticated path ──────────────────────────────────────────
            // The X-Gym-Type header is optional for authenticated users.
            // When provided it MUST match the GymType claim embedded in the JWT.

            if (headerExists && !string.IsNullOrEmpty(headerValue))
            {
                var claimValue = context.User.FindFirstValue("GymType");

                if (int.TryParse(headerValue, out int headerGymType) &&
                    int.TryParse(claimValue,  out int claimGymType))
                {
                    if (headerGymType != claimGymType)
                    {
                        _logger.LogWarning(
                            "GymType mismatch. " +
                            "Header X-Gym-Type: {HeaderGymType}, " +
                            "Claim GymType: {ClaimGymType}, " +
                            "UserId: {UserId}",
                            headerGymType,
                            claimGymType,
                            context.User.FindFirstValue(ClaimTypes.NameIdentifier));

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Forbidden: Gym type mismatch.");
                        return; // ← stop the pipeline, _next is NOT called
                    }
                    // Values match — fall through to _next below
                }
                else
                {
                    // Header present but value is not a valid integer
                    _logger.LogWarning(
                        "Invalid GymType format in header. " +
                        "Header X-Gym-Type: {HeaderValue}, " +
                        "Claim GymType: {ClaimValue}, " +
                        "UserId: {UserId}",
                        (string?)headerValue,
                        context.User.FindFirstValue("GymType"),
                        context.User.FindFirstValue(ClaimTypes.NameIdentifier));

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Bad Request: Invalid GymType format.");
                    return; // ← stop the pipeline, _next is NOT called
                }
            }
            // No header supplied by an authenticated user → accepted; continue
        }
        else
        {
            // ── Anonymous path ──────────────────────────────────────────────
            // Anonymous requests MUST supply a valid X-Gym-Type header (0 or 1).

            if (headerExists && (headerValue == "0" || headerValue == "1"))
            {
                // Valid anonymous gym context — fall through to _next below
            }
            else
            {

                _logger.LogWarning(
                    "Anonymous request missing or has invalid X-Gym-Type header. " +
                    "Header X-Gym-Type: {HeaderValue}",
                    (string?)headerValue);
                _logger.LogWarning(context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Bad Request: Invalid GymType format.");
                return; // ← stop the pipeline, _next is NOT called
            }
        }

        // Single, unconditional call to the next middleware/handler.
        // Reached only when validation passes in BOTH branches above.
        await _next(context);
    }
}
