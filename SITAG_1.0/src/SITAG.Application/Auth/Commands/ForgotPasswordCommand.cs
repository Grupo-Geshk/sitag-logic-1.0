using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Auth.Commands;

/// <summary>
/// Initiates a password reset: generates a secure token, stores its hash,
/// and emails a reset link to the user.
///
/// Always returns success (204) even if the email is not found — this prevents
/// user enumeration attacks.
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : IRequest;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService         _email;
    private readonly IAppSettings          _settings;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext db,
        IEmailService         email,
        IAppSettings          settings)
    {
        _db       = db;
        _email    = email;
        _settings = settings;
    }

    public async Task Handle(ForgotPasswordCommand req, CancellationToken ct)
    {
        var normalised = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Email == normalised && u.IsActive && u.DeletedAt == null, ct);

        // Always return silently — no enumeration
        if (user is null) return;

        // Generate a cryptographically secure raw token
        var rawToken  = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                               .Replace('+', '-').Replace('/', '_').TrimEnd('='); // URL-safe
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        user.PasswordResetTokenHash      = tokenHash;
        user.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
        await _db.SaveChangesAsync(ct);

        var frontendUrl = _settings.FrontendUrl;
        var resetUrl    = $"{frontendUrl}/reset-password?token={rawToken}";

        var html = $"""
            <!DOCTYPE html>
            <html lang="es">
            <body style="font-family:sans-serif;background:#f9fafb;margin:0;padding:32px">
              <div style="max-width:480px;margin:0 auto;background:#fff;border-radius:12px;
                          border:1px solid #e5e7eb;padding:32px">
                <h2 style="color:#1a6b64;margin-top:0">Restablecer contraseña</h2>
                <p style="color:#374151">
                  Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en
                  <strong>SITAG</strong>.
                </p>
                <p style="color:#374151">
                  Haz clic en el botón para crear una nueva contraseña. Este enlace es válido por
                  <strong>1 hora</strong>.
                </p>
                <a href="{resetUrl}"
                   style="display:inline-block;margin:16px 0;padding:12px 24px;
                          background:#3fa79f;color:#fff;border-radius:8px;
                          text-decoration:none;font-weight:600">
                  Restablecer contraseña
                </a>
                <p style="color:#6b7280;font-size:13px">
                  Si no solicitaste este cambio, ignora este correo — tu contraseña no será modificada.
                </p>
                <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0">
                <p style="color:#9ca3af;font-size:12px">SITAG · Sistema de Trazabilidad Ganadera</p>
              </div>
            </body>
            </html>
            """;

        await _email.SendAsync(user.Email, "Restablecer tu contraseña en SITAG", html, ct);
    }
}
