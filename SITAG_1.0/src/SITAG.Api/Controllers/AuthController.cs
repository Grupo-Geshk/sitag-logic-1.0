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
    /// Public self-registration: creates a new Tenant (Plan=Semilla, free) + Producer + owner User.
    /// Returns a token pair so the user is logged in immediately.
    /// Fails with 409 if the email is already in use.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterTenantCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
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

    /// <summary>
    /// Change the authenticated user's own email address.
    /// Requires current password as a security check.
    /// </summary>
    [HttpPost("change-email")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeEmail(
        [FromBody] ChangeEmailCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Request a password-reset email. Always returns 204 to prevent enumeration.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Consume a password-reset token and set a new password.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Validate an invite token and return tenant name + email for pre-filling the form.
    /// Does NOT consume the token.
    /// </summary>
    [HttpGet("invite/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InviteInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateInvite(string token, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ValidateInviteQuery(Uri.UnescapeDataString(token)), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Accept an invite: complete registration (name, password) and join the tenant.
    /// Returns a token pair — the user is logged in immediately.
    /// </summary>
    [HttpPost("accept-invite")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptInviteCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            command with { RawToken = Uri.UnescapeDataString(command.RawToken) },
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
