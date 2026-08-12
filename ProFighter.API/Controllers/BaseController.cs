using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using System.Security.Claims;

namespace ProFighter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    // ── Claim helpers ──────────────────────────────────────────────────────────

    protected Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    protected string? GetCurrentUserRole() =>
        User.FindFirstValue(ClaimTypes.Role);

    protected bool IsInRole(string role) =>
        User.IsInRole(role);

    protected bool IsAdmin() => IsInRole("Admin");

    protected string? GetClientIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    // ── Result → ActionResult mapping ──────────────────────────────────────────

    protected ActionResult<ApiResponse<T>> HandleResult<T>(Result<T> result)
    {
        var apiResponse = result.IsSuccess
            ? ApiResponse<T>.CreateSuccessResponse(result.Message ?? "Success", result.Data, result.Status)
            : ApiResponse<T>.CreateErrorResponse(
                result.Message ?? "Error",
                BuildError(result.Errors, result.Message),
                result.Status);

        return MapStatusCode(result.Status, apiResponse);
    }

    protected ActionResult<ApiResponse<object>> HandleResult(Result result)
    {
        var apiResponse = result.IsSuccess
            ? ApiResponse<object>.CreateSuccessResponse(result.Message ?? "Success", null, result.Status)
            : ApiResponse<object>.CreateErrorResponse(
                result.Message ?? "Error",
                BuildError(result.Errors, result.Message),
                result.Status);

        return MapStatusCode(result.Status, apiResponse);
    }

    protected List<string> GetValidationErrors() =>
        ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

    // ── Private helpers ───────────────────────────────────────────────────────

    private static ErrorResponse BuildError(List<string>? errors, string? message) =>
        errors?.Count > 0
            ? new ErrorResponse("Error", errors)
            : new ErrorResponse("Error", message ?? "An error occurred.");

    private ActionResult<ApiResponse<T>> MapStatusCode<T>(int status, ApiResponse<T> response) =>
        status switch
        {
            200 => Ok(response),
            201 => StatusCode(201, response),
            400 => BadRequest(response),
            401 => Unauthorized(response),
            403 => StatusCode(403, response),
            404 => NotFound(response),
            409 => Conflict(response),
            423 => StatusCode(423, response),
            500 => StatusCode(500, response),
            _   => StatusCode(status, response)
        };
}
