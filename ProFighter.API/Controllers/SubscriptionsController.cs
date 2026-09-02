using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using ProFighter.Application.Subscriptions.Commands.CreateSubscription;
using ProFighter.Application.Subscriptions.Queries.GetCustomerSubscriptions;
using ProFighter.Application.Subscriptions.Queries.GetMySubscriptions;
using ProFighter.Application.Subscriptions.Queries.GetSubscriptions;
using ProFighter.Application.Subscriptions.Commands.SyncSubscriptions;
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
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
            return Unauthorized(ApiResponse<object>.CreateErrorResponse(
                "Unauthorized",
                new ErrorResponse("Unauthorized", "User identity could not be resolved from the token."),
                401));

        var command = new CreateSubscriptionCommand(customerId.Value, (SubscriptionType)1, request.PriceId, request.Quantity);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<CreateSubscriptionResult>.CreateSuccessResponse("Subscription created successfully.", result, 200));
    }

    /// <summary>
    /// Returns the allowed SubscriptionStatus enum values that can be used as a status filter.
    /// </summary>
    [HttpGet("statuses")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IEnumerable<string>>> GetStatuses()
    {
        var statuses = Enum.GetNames<SubscriptionStatus>();
        return Ok(ApiResponse<IEnumerable<string>>.CreateSuccessResponse("Subscription statuses retrieved successfully.", statuses, 200));
    }

    /// <summary>
    /// Get the authenticated user's own subscriptions from the local database.
    /// CustomerId is resolved from the JWT — the client cannot supply another user's ID.
    /// Optionally filter by status (e.g. Active, Pending, Expired). No filter = returns all statuses.
    /// Results are sorted by EndDate ASC and paginated.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetMySubscriptionsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetMySubscriptionsResult>>> GetMySubscriptions(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
            return Unauthorized(ApiResponse<GetMySubscriptionsResult>.CreateErrorResponse(
                "Unauthorized",
                new ErrorResponse("Unauthorized", new List<string> { "User identity could not be resolved from the token." }),
                401));

        var query = new GetMySubscriptionsQuery(
            CustomerId: customerId.Value,
            Status:     status,
            Page:       page,
            PageSize:   pageSize);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<GetMySubscriptionsResult>.CreateSuccessResponse(
            "Subscriptions retrieved successfully.", result, 200));
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
    /// Sync subscriptions from Rekaz to the local database.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<SyncSubscriptionsResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SyncSubscriptionsResult>>> SyncSubscriptions(CancellationToken cancellationToken = default)
    {
        var command = new SyncSubscriptionsCommand();
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<SyncSubscriptionsResult>.CreateSuccessResponse("Subscriptions synchronized successfully.", result, 200));
    }

    /// <summary>
    /// Enqueue a one-time Hangfire job to backfill the Name column for all existing subscriptions where Name is NULL.
    /// </summary>
    [HttpPost("backfill-names")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<string>> BackfillNames()
    {
        var jobId = Hangfire.BackgroundJob.Enqueue<ProFighter.Application.Subscriptions.Jobs.SubscriptionNameBackfillJob>(
            job => job.RunAsync(CancellationToken.None));

        return Ok(ApiResponse<string>.CreateSuccessResponse(
            "Subscription name backfill job enqueued successfully.",
            $"Hangfire Job ID: {jobId}",
            200));
    }
}

public record CreateSubscriptionRequest(Guid PriceId, int Quantity);
