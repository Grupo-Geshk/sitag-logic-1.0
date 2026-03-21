using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Api.Filters;
using SITAG.Application.Workers.Commands;
using SITAG.Application.Workers.Queries;
using SITAG.Domain.Enums;

namespace SITAG.Api.Controllers;

[Route("workers")]
[Authorize]
[RequiresPlan(TenantPlan.Profesional)]
public sealed class WorkersController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search, [FromQuery] WorkerStatus? status,
        [FromQuery] Guid? farmId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetWorkersQuery(search, status, farmId, page, pageSize), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetWorkerByIdQuery(id), ct));

    [HttpGet("{id:guid}/assignment-history")]
    public async Task<IActionResult> AssignmentHistory(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetWorkerAssignmentHistoryQuery(id), ct));

    [HttpGet("{id:guid}/activity-timeline")]
    public async Task<IActionResult> ActivityTimeline(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetWorkerActivityTimelineQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkerCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkerRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new UpdateWorkerCommand(id, body.Name, body.RoleLabel, body.Contact), ct));

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new SetWorkerStatusCommand(id, WorkerStatus.Activo), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new SetWorkerStatusCommand(id, WorkerStatus.Inactivo), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/assign-farms")]
    public async Task<IActionResult> AssignFarms(Guid id, [FromBody] FarmIdsRequest body, CancellationToken ct)
    {
        await Sender.Send(new AssignWorkerFarmsCommand(id, body.FarmIds), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/unassign-farms")]
    public async Task<IActionResult> UnassignFarms(Guid id, [FromBody] FarmIdsRequest body, CancellationToken ct)
    {
        await Sender.Send(new UnassignWorkerFarmsCommand(id, body.FarmIds), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteWorkerCommand(id), ct);
        return NoContent();
    }

    // ── Payments ─────────────────────────────────────────────────────────────
    [HttpGet("{id:guid}/payments")]
    public async Task<IActionResult> GetPayments(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetWorkerPaymentsQuery(id), ct));

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> CreatePayment(
        Guid id, [FromBody] CreateWorkerPaymentRequest body, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateWorkerPaymentCommand(
            id, body.Mode, body.PaymentDate, body.Rate, body.Quantity, body.Notes, body.FarmId), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}

public sealed record UpdateWorkerRequest(string Name, string? RoleLabel, string? Contact);
public sealed record FarmIdsRequest(List<Guid> FarmIds);
public sealed record CreateWorkerPaymentRequest(
    PaymentMode Mode, DateOnly PaymentDate,
    decimal Rate, decimal Quantity,
    string? Notes, Guid? FarmId);
