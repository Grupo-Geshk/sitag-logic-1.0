using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Auth.Commands;
using SITAG.Application.Auth.Dtos;
using SITAG.Application.Auth.Queries;

namespace SITAG.Api.Controllers;

[ApiController]
[Route("auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    /// <summary>
    /// Authenticate with email + password.
    /// Returns a short-lived JWT access token and a long-lived refresh token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Exchange a valid refresh token for a new token pair (rotation).
    /// The old refresh token is immediately revoked.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Revoke the supplied refresh token (logout).
    /// Idempotent — returns 204 even if the token was already revoked or unknown.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get the current authenticated user's profile.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bootstrap: create the very first AdminSistema user + its Tenant.
    /// Returns a token pair so the admin can start using the API immediately.
    /// Fails with 409 if an admin already exists.
    /// </summary>
    [HttpPost("register-admin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAdmin(
        [FromBody] RegisterAdminCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Change the authenticated user's own password (REQ-ONBOARDING-02).
    /// Clears the MustChangePassword flag on success.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }
}
