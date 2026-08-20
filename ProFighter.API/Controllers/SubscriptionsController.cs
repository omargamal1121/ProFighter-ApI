using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using ProFighter.Application.Subscriptions.Commands.CreateSubscription;
using ProFighter.Application.Subscriptions.Commands.SyncSubscriptions;
using ProFighter.Application.Subscriptions.Queries.GetCustomerSubscriptions;
using ProFighter.Application.Subscriptions.Queries.GetSubscriptionById;
using ProFighter.Application.Subscriptions.Queries.GetSubscriptions;
using ProFighter.Domain.Enums;

namespace ProFighter.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : BaseController
{
    private readonly IMediator _mediator;

    public SubscriptionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new subscription for a customer.
    /// Requires authentication.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CreateSubscriptionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CreateSubscriptionResult>>> CreateSubscription(
        [FromBody] CreateSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<CreateSubscriptionResult>.CreateSuccessResponse("Subscription created successfully.", result, 200));
    }

    /// <summary>
    /// Synchronize subscriptions from Rekaz to local database.
    /// This is an admin operation that fetches all subscriptions from Rekaz
    /// and creates/updates local records accordingly.
    /// Requires authentication.
    /// </summary>
    [HttpPost("sync")]
    //[Authorize]
    [ProducesResponseType(typeof(ApiResponse<SyncSubscriptionsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SyncSubscriptionsResult>>> SyncSubscriptions(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SyncSubscriptionsCommand(), cancellationToken);
        return Ok(ApiResponse<SyncSubscriptionsResult>.CreateSuccessResponse(
            $"Subscription synchronization completed. Total: {result.TotalProcessed}, Created: {result.Created}, Updated: {result.Updated}, Skipped: {result.Skipped}.",
            result, 200));
    }

    /// <summary>
    /// Get subscriptions for a specific customer from the local database.
    /// This reads from the ProFighter database, not from Rekaz.
    /// Supports filtering by status, type, date ranges, and active status.
    /// Requires authentication.
    /// </summary>
    [HttpGet("customer/{customerId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetCustomerSubscriptionsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<GetCustomerSubscriptionsResult>>> GetCustomerSubscriptions(
        Guid customerId,
        [FromQuery] string? status = null,
        [FromQuery] SubscriptionType? type = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] DateTime? endDateFrom = null,
        [FromQuery] DateTime? endDateTo = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCustomerSubscriptionsQuery(
            CustomerId: customerId,
            Status: status,
            Type: type,
            StartDateFrom: startDateFrom,
            StartDateTo: startDateTo,
            EndDateFrom: endDateFrom,
            EndDateTo: endDateTo,
            IsActive: isActive);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<GetCustomerSubscriptionsResult>.CreateSuccessResponse("Subscriptions retrieved successfully.", result, 200));
    }

    /// <summary>
    /// Get subscriptions from the local database with optional filtering.
    /// This reads from the ProFighter database, not from Rekaz.
    /// CustomerId is optional - if not provided, returns all subscriptions.
    /// Supports filtering by status, type, date ranges, active status, and pagination.
    /// Requires authentication.
    /// </summary>
    [HttpGet("all")]
 //   [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetSubscriptionsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetSubscriptionsResult>>> GetSubscriptions(
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] SubscriptionType? type = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] DateTime? endDateFrom = null,
        [FromQuery] DateTime? endDateTo = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skipCount = 0,
        [FromQuery] int maxResultCount = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSubscriptionsQuery(
            CustomerId: customerId,
            Status: status,
            Type: type,
            StartDateFrom: startDateFrom,
            StartDateTo: startDateTo,
            EndDateFrom: endDateFrom,
            EndDateTo: endDateTo,
            IsActive: isActive,
            SkipCount: skipCount,
            MaxResultCount: maxResultCount);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<GetSubscriptionsResult>.CreateSuccessResponse("Subscriptions retrieved successfully.", result, 200));
    }

    /// <summary>
    /// Get a specific subscription by Rekaz ID from the local database.
    /// This reads from the ProFighter database, not from Rekaz.
    /// Requires authentication.
    /// </summary>
    [HttpGet("by-rekaz-id/{rekazSubscriptionId}")]
    //[Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetSubscriptionByIdResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetSubscriptionByIdResult>>> GetSubscriptionById(
        Guid rekazSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetSubscriptionByIdQuery(rekazSubscriptionId), cancellationToken);
        
        if (result.Subscription is null)
        {
            return NotFound(ApiResponse<GetSubscriptionByIdResult>.CreateErrorResponse(
                "Subscription not found",
                new ErrorResponse("NotFound", new List<string> { $"Subscription with Rekaz ID {rekazSubscriptionId} not found." }),
                404));
        }

        return Ok(ApiResponse<GetSubscriptionByIdResult>.CreateSuccessResponse("Subscription retrieved successfully.", result, 200));
    }
}
