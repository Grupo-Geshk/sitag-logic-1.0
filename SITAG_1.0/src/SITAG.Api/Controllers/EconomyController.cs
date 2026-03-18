using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Economy.Commands;
using SITAG.Application.Economy.Queries;
using SITAG.Domain.Enums;
using SITAG.Application.Economy.Dtos;

namespace SITAG.Api.Controllers;

[Route("economia")]
[Authorize]
public sealed class EconomyController : ApiControllerBase
{
    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategories(
        [FromQuery] TransactionType? type, CancellationToken ct) =>
        Ok(await Sender.Send(new GetCategoriesQuery(type), ct));

    [HttpPost("categorias")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPatch("categorias/{id:guid}/activate")]
    public async Task<IActionResult> ActivateCategory(Guid id, CancellationToken ct)
    {
        await Sender.Send(new SetCategoryActiveCommand(id, IsActive: true), ct);
        return NoContent();
    }

    [HttpPatch("categorias/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateCategory(Guid id, CancellationToken ct)
    {
        await Sender.Send(new SetCategoryActiveCommand(id, IsActive: false), ct);
        return NoContent();
    }

    [HttpGet("transacciones")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] TransactionType? type, [FromQuery] Guid? farmId,
        [FromQuery] Guid? categoryId,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetTransactionsQuery(type, farmId, categoryId, from, to, page, pageSize), ct));

    [HttpPost("transaccion")]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("transaccion/{id:guid}")]
    public async Task<IActionResult> DeleteTransaction(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteTransactionCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Economic trends grouped by week or month (REQ-ECON-03).
    /// period = "weekly" | "monthly"
    /// </summary>
    [HttpGet("tendencias")]
    public async Task<IActionResult> Trends(
        [FromQuery] string  period    = "monthly",
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate   = null,
        [FromQuery] Guid?   farmId    = null,
        CancellationToken ct = default)
    {
        var from = startDate ?? DateTimeOffset.UtcNow.AddMonths(-6);
        var to   = endDate   ?? DateTimeOffset.UtcNow;
        return Ok(await Sender.Send(new GetEconomyTrendsQuery(period, from, to, farmId), ct));
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Summary(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] Guid? farmId, CancellationToken ct)
    {
        var f = from ?? DateTimeOffset.UtcNow.AddMonths(-1);
        var t = to   ?? DateTimeOffset.UtcNow;
        return Ok(await Sender.Send(new GetEconomySummaryQuery(f, t, farmId), ct));
    }
}
