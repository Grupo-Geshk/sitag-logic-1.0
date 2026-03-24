using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Admin.Commands;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Admin.Queries;
using SITAG.Application.Common.Dtos;
using SITAG.Application.Producers.Dtos;
using SITAG.Domain.Enums;

namespace SITAG.Api.Controllers;

[Route("admin/tenants")]
[Authorize(Roles = nameof(UserRole.AdminSistema))]
public sealed class AdminTenantsController : ApiControllerBase
{
    /// <summary>
    /// List all tenants with optional search and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetTenantsQuery(search, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Get a single tenant with user count.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetTenantByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a new tenant + producer + initial owner user (REQ-ONBOARDING-01).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update a tenant's subscription status and/or paidUntil date (REQ-TENANT-03, REQ-TENANT-06).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateTenantStatusRequest body,
        CancellationToken ct)
    {
        await Sender.Send(new UpdateTenantStatusCommand(
            TenantId     : id,
            Status       : body.Status,
            PaidUntil    : body.PaidUntil,
            Note         : body.Note,
            ActorUserId  : CurrentUserId,
            ActorEmail   : CurrentUserEmail), ct);

        return NoContent();
    }

    /// <summary>
    /// Get the immutable audit log for a tenant (REQ-TENANT-05).
    /// </summary>
    [HttpGet("{id:guid}/audit")]
    [ProducesResponseType(typeof(IReadOnlyList<TenantAuditEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditLog(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetTenantAuditLogQuery(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// List all producers (one per tenant).
    /// </summary>
    [HttpGet("/admin/producers")]
    [ProducesResponseType(typeof(IReadOnlyList<ProducerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducers(CancellationToken ct)
    {
        var result = await Sender.Send(new GetProducersQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Update a tenant's subscription plan.
    /// </summary>
    [HttpPut("{id:guid}/plan")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlan(
        Guid id,
        [FromBody] UpdateTenantPlanRequest body,
        CancellationToken ct)
    {
        await Sender.Send(new UpdateTenantPlanCommand(id, body.Plan), ct);
        return NoContent();
    }

    /// <summary>
    /// Returns a count summary of all data owned by a tenant (for pre-deletion review).
    /// </summary>
    [HttpGet("{id:guid}/summary")]
    [ProducesResponseType(typeof(TenantSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetTenantSummaryQuery(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// Hard-delete ALL data for a tenant. Irreversible.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteTenantCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Log that the admin manually sent a payment reminder (REQ-TENANT-08).
    /// </summary>
    [HttpPost("{id:guid}/reminder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogReminder(
        Guid id,
        [FromBody] LogReminderRequest body,
        CancellationToken ct)
    {
        await Sender.Send(new LogReminderCommand(
            TenantId    : id,
            Note        : body.Note,
            ActorUserId : CurrentUserId,
            ActorEmail  : CurrentUserEmail), ct);

        return NoContent();
    }
}

// ── Request body records (thin input models, separate from commands) ─────────
public sealed record UpdateTenantStatusRequest(
    TenantStatus Status,
    DateTimeOffset? PaidUntil,
    string? Note);

public sealed record UpdateTenantPlanRequest(TenantPlan Plan);

public sealed record LogReminderRequest(string? Note);
