using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using ProFighter.Application.Customers.Queries.GetMyProfile;

namespace ProFighter.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public sealed class CustomersController : BaseController
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get the authenticated customer's profile.
    /// CustomerId is resolved from the JWT — the client cannot supply another user's ID.
    /// Returns name, mobile number, email, email confirmation status, loyalty points, source, and timestamps.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<GetMyProfileResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<GetMyProfileResult>>> GetMyProfile(
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
            return Unauthorized(ApiResponse<object>.CreateErrorResponse(
                "Unauthorized",
                new ErrorResponse("Unauthorized", "User identity could not be resolved from the token."),
                401));

        var query  = new GetMyProfileQuery(CustomerId: customerId.Value);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<GetMyProfileResult>.CreateSuccessResponse(
            "Profile retrieved successfully.", result, 200));
    }
}
