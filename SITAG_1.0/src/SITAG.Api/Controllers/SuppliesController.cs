using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Api.Filters;
using SITAG.Application.Supplies.Commands;
using SITAG.Application.Supplies.Queries;
using SITAG.Domain.Enums;

namespace SITAG.Api.Controllers;

[Route("insumos")]
[Authorize]
[RequiresPlan(TenantPlan.Profesional)]
public sealed class SuppliesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? farmId, [FromQuery] bool? lowStockOnly,
        CancellationToken ct) =>
        Ok(await Sender.Send(new GetSuppliesQuery(farmId, lowStockOnly), ct));

    [HttpGet("alertas")]
    public async Task<IActionResult> GetAlerts([FromQuery] Guid? farmId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetSupplyAlertsQuery(farmId), ct));

    /// <summary>
    /// Supply consumption report grouped by supply for a given period (REQ-SUPPLY-04).
    /// </summary>
    [HttpGet("uso")]
    public async Task<IActionResult> UsageReport(
        [FromQuery] Guid?           farmId,
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetSupplyUsageQuery(farmId, startDate, endDate), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetSupplyByIdQuery(id), ct));

    [HttpGet("{id:guid}/movimientos")]
    public async Task<IActionResult> GetMovements(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetSupplyMovementsQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplyCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplyRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new UpdateSupplyCommand(
            id, body.Name, body.Category, body.Unit, body.MinStockLevel, body.ExpirationDate), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteSupplyCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/ajuste")]
    public async Task<IActionResult> Adjust(Guid id, [FromBody] AdjustStockRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new AdjustStockCommand(id, body.MovementType, body.Quantity, body.Reason), ct));

    // ── Lots ─────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/lotes")]
    public async Task<IActionResult> GetLots(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetSupplyLotsQuery(id), ct));

    [HttpPost("{id:guid}/lotes")]
    public async Task<IActionResult> CreateLot(Guid id, [FromBody] CreateSupplyLotRequest body, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateSupplyLotCommand(
            id, body.Quantity, body.UnitCost, body.Supplier,
            body.ExpirationDate, body.PurchaseDate, body.Notes), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}/lotes/{lotId:guid}")]
    public async Task<IActionResult> UpdateLot(Guid id, Guid lotId, [FromBody] UpdateSupplyLotRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new UpdateSupplyLotCommand(
            lotId, body.UnitCost, body.Supplier, body.ExpirationDate,
            body.PurchaseDate, body.Status, body.Notes), ct));
}

public sealed record UpdateSupplyRequest(
    string Name, string? Category, string Unit,
    decimal MinStockLevel, DateOnly? ExpirationDate);

public sealed record AdjustStockRequest(
    SupplyMovementType MovementType, decimal Quantity, string? Reason);

public sealed record CreateSupplyLotRequest(
    decimal Quantity,
    decimal? UnitCost,
    string? Supplier,
    DateOnly? ExpirationDate,
    DateOnly PurchaseDate,
    string? Notes);

public sealed record UpdateSupplyLotRequest(
    decimal? UnitCost,
    string? Supplier,
    DateOnly? ExpirationDate,
    DateOnly PurchaseDate,
    SupplyLotStatus Status,
    string? Notes);
