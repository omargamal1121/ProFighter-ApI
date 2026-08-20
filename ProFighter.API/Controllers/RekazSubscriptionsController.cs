using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Models;
using ProFighter.Application.Subscriptions.Commands.CreateRekazSubscription;
using ProFighter.Application.Subscriptions.Queries.GetRekazSubscriptionById;
using ProFighter.Application.Subscriptions.Queries.GetRekazSubscriptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.API.Controllers;

[Route("api/rekaz/subscriptions")]
public class RekazSubscriptionsController : BaseController
{
    private readonly IMediator _mediator;

    public RekazSubscriptionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Fetches subscriptions from the Rekaz platform.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<RekazSubscriptionsListResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ApiResponse<RekazSubscriptionsListResult>>> GetSubscriptions(
        [FromQuery] int maxResultCount = 20,
        [FromQuery] Guid? customerId = null,
        [FromQuery] DateTime? startAtMin = null,
        [FromQuery] DateTime? startAtMax = null,
        [FromQuery] DateTime? nextBillingAtMin = null,
        [FromQuery] DateTime? nextBillingAtMax = null,
        [FromQuery] List<string>? statuses = null,
        [FromQuery] string? customerMobile = null,
        [FromQuery] string? keyword = null,
        [FromQuery] List<Guid>? priceIds = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] string? sorting = null,
        [FromQuery] int skipCount = 0,
        CancellationToken ct = default)
    {
        var query = new GetRekazSubscriptionsQuery(
            MaxResultCount: maxResultCount,
            CustomerId: customerId,
            StartAtMin: startAtMin,
            StartAtMax: startAtMax,
            NextBillingAtMin: nextBillingAtMin,
            NextBillingAtMax: nextBillingAtMax,
            Statuses: statuses,
            CustomerMobile: customerMobile,
            Keyword: keyword,
            PriceIds: priceIds,
            BranchId: branchId,
            Sorting: sorting,
            SkipCount: skipCount
        );

        var data = await _mediator.Send(query, ct);
        return HandleResult(Result<RekazSubscriptionsListResult>.Success(data, "Subscriptions fetched successfully."));
    }

    /// <summary>
    /// Fetches a single subscription by ID from the Rekaz platform.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RekazSubscriptionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RekazSubscriptionResult>>> GetSubscriptionById(Guid id, CancellationToken ct = default)
    {
        var data = await _mediator.Send(new GetRekazSubscriptionByIdQuery(id), ct);
        if (data == null)
        {
            return NotFound(ApiResponse<object>.CreateErrorResponse("Subscription not found.",new ErrorResponse("Invaid data","Rekaz"),500));
        }
        return HandleResult(Result<RekazSubscriptionResult>.Success(data, "Subscription fetched successfully."));
    }

    /// <summary>
    /// Creates a new subscription in the Rekaz platform.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RekazSubscriptionCreatedResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RekazSubscriptionCreatedResult>>> CreateSubscription(
        [FromBody] CreateRekazSubscriptionRequest request,
        CancellationToken ct = default)
    {
        var data = await _mediator.Send(new CreateRekazSubscriptionCommand(request), ct);
        return HandleResult(Result<RekazSubscriptionCreatedResult>.Success(data, "Subscription created successfully."));
    }
}
