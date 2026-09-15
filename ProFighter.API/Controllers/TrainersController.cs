using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using ProFighter.Application.Trainers.Common;
using ProFighter.Application.Trainers.Queries.GetTrainerById;
using ProFighter.Application.Trainers.Queries.GetTrainers;
using ProFighter.Domain.Enums;

namespace ProFighter.API.Controllers;

[ApiController]
[Route("api/trainers")]
public class TrainersController : BaseController
{
    private readonly ISender _mediator;

    public TrainersController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets public list of active trainers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TrainerDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TrainerDto>>>> GetTrainers(
        [FromQuery] GymType? gymType,
        [FromQuery] string? searchTerm,
        CancellationToken ct)
    {
        var query = new GetTrainersQuery(GymType: gymType, IsActive: true, SearchTerm: searchTerm);
        var result = await _mediator.Send(query, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets public details of a specific trainer by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TrainerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TrainerDto>>> GetTrainerById(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTrainerByIdQuery(id), ct);
        return HandleResult(result);
    }
}
