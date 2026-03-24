using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Auth.Commands;

/// <summary>
/// Validates a password-reset token and sets the new password.
/// Invalidates the token on success (single-use).
/// </summary>
public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword) : IRequest;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher       _hasher;

    public ResetPasswordCommandHandler(IApplicationDbContext db, IPasswordHasher hasher)
    {
        _db     = db;
        _hasher = hasher;
    }

    public async Task Handle(ResetPasswordCommand req, CancellationToken ct)
    {
        if (req.NewPassword.Length < 8)
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");

        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(req.Token)));

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.PasswordResetTokenHash == tokenHash && u.DeletedAt == null, ct);

        if (user is null)
            throw new InvalidOperationException("El enlace de restablecimiento no es válido.");

        if (user.PasswordResetTokenExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("El enlace de restablecimiento ha expirado. Solicita uno nuevo.");

        user.PasswordHash                = _hasher.Hash(req.NewPassword);
        user.MustChangePassword          = false;
        user.PasswordResetTokenHash      = null;   // invalidate — single use
        user.PasswordResetTokenExpiresAt = null;

        await _db.SaveChangesAsync(ct);
    }
}
