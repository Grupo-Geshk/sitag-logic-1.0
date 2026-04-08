using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Commands;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Admin.Queries;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Api.Controllers;

/// <summary>
/// Producer-facing user management for the authenticated user's own tenant.
/// Producers can list, activate/deactivate, and invite users within their tenant.
/// </summary>
[Route("tenants/me")]
[Authorize]
public sealed class TenantUsersController(
    ICurrentUser currentUser,
    IApplicationDbContext db) : ApiControllerBase
{

    /// <summary>
    /// List all active users in the current tenant.
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var result = await Sender.Send(
            new GetUsersQuery(null, null, currentUser.TenantId, null, 1, 200), ct);
        return Ok(result.Items);
    }

    /// <summary>
    /// Activate a user within the current tenant. Cannot activate yourself.
    /// </summary>
    [HttpPatch("users/{userId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid userId, CancellationToken ct)
    {
        await SetActive(userId, true, ct);
        return NoContent();
    }

    /// <summary>
    /// Deactivate a user within the current tenant. Cannot deactivate yourself.
    /// </summary>
    [HttpPatch("users/{userId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid userId, CancellationToken ct)
    {
        await SetActive(userId, false, ct);
        return NoContent();
    }

    /// <summary>
    /// Create a single-use invite link for a new user to join this tenant.
    /// Validates plan user limits before issuing.
    /// </summary>
    [HttpPost("invites")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInvite(
        [FromBody] TenantInviteRequest body,
        CancellationToken ct)
    {
        var result = await Sender.Send(new CreateInviteCommand(
            TenantId    : currentUser.TenantId,
            Email       : body.Email,
            ActorUserId : currentUser.UserId), ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task SetActive(Guid userId, bool isActive, CancellationToken ct)
    {
        if (userId == currentUser.UserId)
            throw new InvalidOperationException("No puedes modificar tu propio estado.");

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Id == userId && u.TenantId == currentUser.TenantId && u.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Usuario {userId} no encontrado en este tenant.");

        user.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }
}

public sealed record TenantInviteRequest(string Email);
