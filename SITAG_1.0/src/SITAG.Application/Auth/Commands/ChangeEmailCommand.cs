using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Auth.Commands;

/// <summary>
/// Lets the authenticated user change their own email address.
/// Requires the current password as a security check.
///
/// When email confirmation is ready (Resend), un-comment the SendAsync call
/// and add a pending-email / token flow before committing the change.
/// </summary>
public sealed record ChangeEmailCommand(
    string NewEmail,
    string CurrentPassword) : IRequest;

public sealed class ChangeEmailCommandHandler : IRequestHandler<ChangeEmailCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser          _currentUser;
    private readonly IPasswordHasher       _hasher;
    private readonly IEmailService         _email;

    public ChangeEmailCommandHandler(
        IApplicationDbContext db,
        ICurrentUser          currentUser,
        IPasswordHasher       hasher,
        IEmailService         email)
    {
        _db          = db;
        _currentUser = currentUser;
        _hasher      = hasher;
        _email       = email;
    }

    public async Task Handle(ChangeEmailCommand req, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId && u.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!_hasher.Verify(req.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        var newEmail = req.NewEmail.Trim().ToLowerInvariant();

        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The new email is the same as the current one.");

        var taken = await _db.Users.AnyAsync(
            u => u.Email == newEmail && u.Id != user.Id && u.DeletedAt == null, ct);
        if (taken)
            throw new InvalidOperationException("This email address is already in use by another account.");

        user.Email = newEmail;
        await _db.SaveChangesAsync(ct);

        // ── Future: send confirmation / notification via Resend ───────────────
        // await _email.SendAsync(
        //     to:       newEmail,
        //     subject:  "Tu correo en SITAG ha sido actualizado",
        //     htmlBody: $"<p>Tu dirección de correo fue cambiada a <strong>{newEmail}</strong>.</p>",
        //     ct:       ct);
    }
}
