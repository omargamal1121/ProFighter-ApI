using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using ProFighter.Application.Customers.Commands.DeleteCustomerImage;
using ProFighter.Application.Customers.Commands.UploadCustomerImage;
using ProFighter.Application.Customers.Common;
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

    /// <summary>
    /// Upload an image for the authenticated customer.
    /// </summary>
    [HttpPost("me/images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<CustomerMediaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CustomerMediaDto>>> UploadImage(
        [FromForm] UploadCustomerImageRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
            return Unauthorized(ApiResponse<object>.CreateErrorResponse(
                "Unauthorized",
                new ErrorResponse("Unauthorized", "User identity could not be resolved from the token."),
                401));

        var command = new UploadCustomerImageCommand(customerId.Value, request.Image, request.Purpose, request.DisplayOrder);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a specific image for the authenticated customer.
    /// </summary>
    [HttpDelete("me/images/{imageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteImage(
        [FromRoute] Guid imageId,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
            return Unauthorized(ApiResponse<object>.CreateErrorResponse(
                "Unauthorized",
                new ErrorResponse("Unauthorized", "User identity could not be resolved from the token."),
                401));

        var command = new DeleteCustomerImageCommand(customerId.Value, imageId);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

public class UploadCustomerImageRequest
{
    public Microsoft.AspNetCore.Http.IFormFile Image { get; set; } = null!;
    public ProFighter.Domain.Enums.MediaPurpose Purpose { get; set; } = ProFighter.Domain.Enums.MediaPurpose.ProfileImage;
    public int DisplayOrder { get; set; } = 0;
}
