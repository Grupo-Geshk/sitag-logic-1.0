using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Auth.Commands;

/// <summary>
/// Revokes the supplied refresh token. Idempotent — no error if already revoked.
/// </summary>
public sealed record LogoutCommand(string RefreshToken) : IRequest;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IApplicationDbContext _db;

    public LogoutCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var incomingHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));

        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == incomingHash, cancellationToken);

        if (stored is null || stored.IsRevoked)
            return; // already gone — idempotent

        stored.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
