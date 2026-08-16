using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Entities;
using ProFighter.Infrastructure.ExternalServices.Rekaz;
using System.Text.Json;

namespace ProFighter.API.Controllers;

// Route segment is a secret unguessable path — exclude from OpenAPI/Swagger generation
// so it never appears in generated docs or an exposed Swagger UI.
[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[Route("webhooks/rekaz")]
public class RekazWebhookController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly RekazWebhookOptions _webhookOptions;
    private readonly ILogger<RekazWebhookController> _logger;

    public RekazWebhookController(
        IApplicationDbContext context,
        IOptions<RekazWebhookOptions> webhookOptions,
        ILogger<RekazWebhookController> logger)
    {
        _context = context;
        _webhookOptions = webhookOptions.Value;
        _logger = logger;
    }

    [HttpPost("{path}")]
    public async Task<IActionResult> Receive(string path, CancellationToken ct)
    {
        if (path != _webhookOptions.ReceiverPath)
            return NotFound(); // plain 404 — don't reveal whether the path is "close"

        string rawBody;
        using (var reader = new StreamReader(Request.Body))
        {
            rawBody = await reader.ReadToEndAsync(ct);
        }

        if (rawBody.Length > 64 * 1024) // reasonable body-size guard per Rekaz's security guidance
            return BadRequest();

        JsonElement parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<JsonElement>(rawBody);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        if (!parsed.TryGetProperty("Id", out var idProp) || idProp.ValueKind != JsonValueKind.String ||
            !parsed.TryGetProperty("EventName", out var eventNameProp) || eventNameProp.ValueKind != JsonValueKind.String)
        {
            return BadRequest();
        }

        if (!Guid.TryParse(idProp.GetString(), out var eventId))
            return BadRequest();

        var eventName = eventNameProp.GetString()!;

        // Insert-if-absent: Id is the primary key, so duplicate deliveries are naturally
        // detected here rather than treated as an error (Rekaz's docs explicitly warn
        // duplicate deliveries are possible and delivery order is not guaranteed).
        var alreadyExists = await _context.RekazWebhookInboxEntries.AnyAsync(w => w.Id == eventId, ct);
        if (!alreadyExists)
        {
            var entry = new RekazWebhookInboxEntry(eventId, eventName, rawBody);
            _context.RekazWebhookInboxEntries.Add(entry);
            await _context.SaveChangesAsync(ct);

            BackgroundJob.Enqueue<IRekazWebhookProcessor>(p => p.ProcessAsync(eventId, CancellationToken.None));
        }

        // Never log the raw payload (may contain customer PII per Rekaz's own guidance) —
        // log only event metadata.
        _logger.LogInformation("Rekaz webhook received: {EventName} ({EventId})", eventName, eventId);

        return Ok(); // any 2xx counts as success per Rekaz's delivery contract; queue real
                     // processing and acknowledge fast, per their docs.
    }
}
