using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Supplies.Commands;
using SITAG.Application.Supplies.Queries;
using SITAG.Application.VetServices.Commands;
using SITAG.Application.VetServices.Queries;
using SITAG.Domain.Enums;

namespace SITAG.Api.Controllers;

[Route("servicios")]
[Authorize]
public sealed class VetServicesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? farmId, [FromQuery] ServiceStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetVetServicesQuery(farmId, status, page, pageSize), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetVetServiceByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVetServiceCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVetServiceRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new UpdateVetServiceCommand(
            id, body.ServiceType, body.ScheduledDate, body.FarmId, body.DivisionId,
            body.WorkerId, body.Cost, body.Notes, body.AnimalIds), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteVetServiceCommand(id), ct);
        return NoContent();
    }

    [HttpGet("animal/{animalId:guid}")]
    public async Task<IActionResult> GetByAnimal(Guid animalId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetServicesByAnimalQuery(animalId), ct));

    [HttpPatch("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new CompleteVetServiceCommand(id, body.CompletedDate, body.Cost, body.Notes), ct));

    // ── Supply consumptions ───────────────────────────────────────────────────
    [HttpGet("{id:guid}/consumos")]
    public async Task<IActionResult> GetConsumptions(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetServiceConsumptionsQuery(id), ct));

    [HttpPost("{id:guid}/consumos")]
    public async Task<IActionResult> AddConsumption(Guid id, [FromBody] AddConsumptionRequest body, CancellationToken ct)
    {
        var result = await Sender.Send(new AddServiceConsumptionCommand(id, body.SupplyId, body.Quantity, body.LotId), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}

public sealed record UpdateVetServiceRequest(
    string ServiceType, DateTimeOffset ScheduledDate,
    Guid FarmId, Guid? DivisionId, Guid? WorkerId,
    decimal? Cost, string? Notes, List<Guid> AnimalIds);
public sealed record CompleteRequest(DateTimeOffset? CompletedDate, decimal? Cost, string? Notes);
public sealed record AddConsumptionRequest(Guid SupplyId, decimal Quantity, Guid? LotId = null);
