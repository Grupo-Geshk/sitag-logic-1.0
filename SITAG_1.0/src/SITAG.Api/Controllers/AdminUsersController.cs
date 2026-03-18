using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Admin.Commands;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Admin.Queries;
using SITAG.Application.Common.Dtos;
using SITAG.Domain.Enums;

namespace SITAG.Api.Controllers;

[Route("admin/users")]
[Authorize(Roles = nameof(UserRole.AdminSistema))]
public sealed class AdminUsersController : ApiControllerBase
{
    /// <summary>
    /// Paginated list of all users with optional filters (REQ-USER-02).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string?   search,
        [FromQuery] UserRole? role,
        [FromQuery] Guid?     tenantId,
        [FromQuery] bool?     isActive,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetUsersQuery(search, role, tenantId, isActive, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Get a single user by ID (REQ-USER-02).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetUserByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a new user under an existing tenant (REQ-USER-01).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Edit a user's profile (REQ-USER-03).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest body,
        CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateUserCommand(
            UserId    : id,
            FirstName : body.FirstName,
            LastName  : body.LastName,
            Phone     : body.Phone,
            Email     : body.Email,
            Role      : body.Role,
            IsActive  : body.IsActive), ct);

        return Ok(result);
    }

    /// <summary>
    /// Activate a user account (REQ-USER-04).
    /// </summary>
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new SetUserActiveCommand(id, IsActive: true), ct);
        return NoContent();
    }

    /// <summary>
    /// Deactivate a user account (REQ-USER-04).
    /// </summary>
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new SetUserActiveCommand(id, IsActive: false), ct);
        return NoContent();
    }

    /// <summary>
    /// Soft-delete a user (REQ-USER-05).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteUserCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Global system statistics (REQ-USER-06).
    /// </summary>
    [HttpGet("/admin/statistics")]
    [ProducesResponseType(typeof(AdminStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Statistics(CancellationToken ct)
    {
        var result = await Sender.Send(new GetAdminStatisticsQuery(), ct);
        return Ok(result);
    }
}

// ── Request body record ──────────────────────────────────────────────────────
public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? Phone,
    string Email,
    UserRole Role,
    bool IsActive);
