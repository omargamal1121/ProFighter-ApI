using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using ProFighter.Application.Trainers.Commands.ActivateTrainer;
using ProFighter.Application.Trainers.Commands.CreateTrainer;
using ProFighter.Application.Trainers.Commands.DeactivateTrainer;
using ProFighter.Application.Trainers.Commands.DeleteTrainer;
using ProFighter.Application.Trainers.Commands.DeleteTrainerImage;
using ProFighter.Application.Trainers.Commands.UpdateTrainer;
using ProFighter.Application.Trainers.Commands.UploadTrainerImage;
using ProFighter.Application.Trainers.Common;
using ProFighter.Application.Trainers.Queries.GetTrainerById;
using ProFighter.Application.Trainers.Queries.GetTrainers;
using ProFighter.Domain.Enums;

namespace ProFighter.API.Controllers;

[ApiController]
[Route("api/admin/trainers")]
[Authorize(Roles = "Admin")]
public class AdminTrainersController : BaseController
{
    private readonly ISender _mediator;

    public AdminTrainersController(ISender mediator)
    {
        _mediator = mediator;
    }

    // ── TRAINER CRUD ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new trainer. Providing an image activates the trainer immediately;
    /// otherwise the trainer is created as inactive.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<TrainerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TrainerDto>>> CreateTrainer(
        [FromForm] CreateTrainerRequest request,
        CancellationToken ct)
    {
        var command = new CreateTrainerCommand(
            request.Name,
            request.Specialization,
            request.Bio,
            request.GymType,
            request.Image);

        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    /// <summary>Gets all trainers with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TrainerDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TrainerDto>>>> GetTrainers(
        [FromQuery] GymType? gymType,
        [FromQuery] bool? isActive,
        [FromQuery] string? searchTerm,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTrainersQuery(gymType, isActive, searchTerm), ct);
        return HandleResult(result);
    }

    /// <summary>Gets a single trainer by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TrainerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TrainerDto>>> GetTrainerById(
        [FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTrainerByIdQuery(id), ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Updates trainer text data only (name, specialization, bio, gym type).
    /// Use the image endpoints below to manage images.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TrainerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TrainerDto>>> UpdateTrainer(
        [FromRoute] Guid id,
        [FromBody] UpdateTrainerRequest request,
        CancellationToken ct)
    {
        var command = new UpdateTrainerCommand(id, request.Name, request.Specialization, request.Bio, request.GymType);
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    /// <summary>Deletes a trainer and all associated Cloudinary images.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTrainer(
        [FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteTrainerCommand(id), ct);
        return HandleResult(result);
    }

    // ── ACTIVATE / DEACTIVATE ────────────────────────────────────────────────

    /// <summary>
    /// Activates a trainer. Requires at least one image — returns 422 otherwise.
    /// </summary>
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<TrainerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), 422)]
    public async Task<ActionResult<ApiResponse<TrainerDto>>> ActivateTrainer(
        [FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ActivateTrainerCommand(id), ct);
        return HandleResult(result);
    }

    /// <summary>Deactivates a trainer manually.</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<TrainerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TrainerDto>>> DeactivateTrainer(
        [FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeactivateTrainerCommand(id), ct);
        return HandleResult(result);
    }

    // ── TRAINER IMAGES ───────────────────────────────────────────────────────

    /// <summary>Uploads an image for a trainer (gallery, profile, certificate).</summary>
    [HttpPost("{id:guid}/images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<TrainerMediaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TrainerMediaDto>>> UploadTrainerImage(
        [FromRoute] Guid id,
        [FromForm] UploadTrainerImageRequest request,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UploadTrainerImageCommand(id, request.Image, request.Purpose, request.DisplayOrder), ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Deletes a specific trainer image. Auto-deactivates the trainer if no images remain.
    /// </summary>
    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTrainerImage(
        [FromRoute] Guid id,
        [FromRoute] Guid imageId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteTrainerImageCommand(id, imageId), ct);
        return HandleResult(result);
    }
}

// ── Request DTO Models for Form & Body Binding ────────────────────────────

public class CreateTrainerRequest
{
    public string Name { get; set; } = null!;
    public SubscriptionType Specialization { get; set; }
    public string? Bio { get; set; }
    public GymType GymType { get; set; } = GymType.ProFighter;
    public IFormFile? Image { get; set; }
}

public class UploadTrainerImageRequest
{
    public IFormFile Image { get; set; } = null!;
    public MediaPurpose Purpose { get; set; } = MediaPurpose.Gallery;
    public int DisplayOrder { get; set; } = 0;
}

public record UpdateTrainerRequest(
    string Name,
    SubscriptionType Specialization,
    string? Bio,
    GymType? GymType
);
