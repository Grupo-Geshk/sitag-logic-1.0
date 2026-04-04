using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Animals.Commands;
using SITAG.Application.Animals.Dtos;
using SITAG.Application.Animals.Queries;
using SITAG.Application.Common.Dtos;
using SITAG.Application.Farms.Commands;
using SITAG.Domain.Enums;

namespace SITAG.Api.Controllers;

[Route("animals")]
[Authorize]
public sealed class AnimalsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? farmId, [FromQuery] Guid? divisionId,
        [FromQuery] AnimalStatus? status, [FromQuery] string? sex,
        [FromQuery] AnimalHealthStatus? healthStatus,
        [FromQuery] int? ageMin, [FromQuery] int? ageMax,
        [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetAnimalsQuery(
            farmId, divisionId, status, sex, healthStatus, ageMin, ageMax, search, page, pageSize), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetAnimalByIdQuery(id), ct));

    [HttpGet("{id:guid}/genealogy")]
    public async Task<IActionResult> GetGenealogy(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetAnimalGenealogyQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAnimalCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase([FromBody] PurchaseAnimalCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAnimalRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new UpdateAnimalCommand(
            id, body.Name, body.Breed, body.Sex, body.BirthDate, body.Weight,
            body.FarmId, body.DivisionId,
            body.PhotoUrl, body.MotherRef, body.FatherRef, body.Color), ct));

    [HttpPatch("{id:guid}/health")]
    public async Task<IActionResult> UpdateHealth(Guid id, [FromBody] UpdateHealthRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new UpdateAnimalHealthCommand(id, body.HealthStatus, body.Notes), ct));

    [HttpPatch("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseAnimalRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new CloseAnimalCommand(id, body.Outcome, body.Reason), ct));

    [HttpPatch("{id:guid}/tag")]
    public async Task<IActionResult> AssignTag(Guid id, [FromBody] AssignTagRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new AssignTagCommand(id, body.TagNumber), ct));

    [HttpPost("bulk-movement")]
    public async Task<IActionResult> BulkMove([FromBody] BulkMoveAnimalsCommand cmd, CancellationToken ct)
    {
        var count = await Sender.Send(cmd, ct);
        return Ok(new { success = true, count });
    }

    // ── Events ───────────────────────────────────────────────────────────────
    [HttpGet("{id:guid}/events")]
    public async Task<IActionResult> GetEvents(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetAnimalEventsQuery(id), ct));

    [HttpPost("{id:guid}/events")]
    public async Task<IActionResult> CreateEvent(Guid id, [FromBody] CreateEventRequest body, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateAnimalEventCommand(
            id, body.EventType, body.EventDate, body.WorkerId, body.Cost, body.Description,
            body.Amount, body.CategoryId, body.Offspring), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{animalId:guid}/events/{eventId:guid}")]
    public async Task<IActionResult> UpdateEvent(
        Guid animalId, Guid eventId,
        [FromBody] UpdateEventRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new UpdateAnimalEventCommand(
            animalId, eventId, body.EventDate, body.WorkerId, body.Cost, body.Description), ct));

    [HttpDelete("{animalId:guid}/events/{eventId:guid}")]
    public async Task<IActionResult> DeleteEvent(Guid animalId, Guid eventId, CancellationToken ct)
    {
        await Sender.Send(new DeleteAnimalEventCommand(animalId, eventId), ct);
        return NoContent();
    }

    // ── Movements ────────────────────────────────────────────────────────────
    [HttpGet("{id:guid}/movements")]
    public async Task<IActionResult> GetMovements(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetAnimalMovementsQuery(id), ct));

    // ── Brand ────────────────────────────────────────────────────────────────
    [HttpPatch("{id:guid}/brand")]
    public async Task<IActionResult> AssignBrand(Guid id, [FromBody] AssignBrandRequest body, CancellationToken ct)
    {
        await Sender.Send(new AssignAnimalBrandCommand(id, body.BrandId, body.BrandedAt), ct);
        return NoContent();
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public sealed record UpdateAnimalRequest(
    string? Name, string? Breed, string Sex,
    DateOnly? BirthDate, decimal? Weight,
    Guid FarmId, Guid? DivisionId,
    string? PhotoUrl = null,
    string? MotherRef = null,
    string? FatherRef = null,
    string? Color = null);

public sealed record UpdateHealthRequest(AnimalHealthStatus HealthStatus, string? Notes);
public sealed record CloseAnimalRequest(AnimalStatus Outcome, string? Reason);
public sealed record AssignTagRequest(string TagNumber);

public sealed record CreateEventRequest(
    AnimalEventType EventType, DateTimeOffset EventDate,
    Guid? WorkerId, decimal? Cost, string? Description,
    decimal? Amount, Guid? CategoryId,
    OffspringData? Offspring = null);

public sealed record UpdateEventRequest(
    DateTimeOffset EventDate, Guid? WorkerId,
    decimal? Cost, string? Description);

public sealed record AssignBrandRequest(Guid? BrandId, DateTimeOffset? BrandedAt);
