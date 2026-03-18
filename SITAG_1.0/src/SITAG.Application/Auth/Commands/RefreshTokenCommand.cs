using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Auth.Dtos;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthTokensDto>;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokensDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITokenService         _tokens;

    public RefreshTokenCommandHandler(IApplicationDbContext db, ITokenService tokens)
    {
        _db     = db;
        _tokens = tokens;
    }

    public async Task<AuthTokensDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var incomingHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));

        var stored = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == incomingHash, cancellationToken);

        if (stored is null || !stored.IsActive)
            throw new UnauthorizedAccessException("Refresh token is invalid or has expired.");

        // Rotate: revoke old, issue new
        stored.RevokedAt = DateTimeOffset.UtcNow;

        var (accessToken, accessExpiry)         = _tokens.GenerateAccessToken(stored.User);
        var (rawRefresh, hashRefresh, rtExpiry) = _tokens.GenerateRefreshToken();

        var newToken = new Domain.Entities.RefreshToken
        {
            UserId    = stored.UserId,
            TenantId  = stored.TenantId,
            TokenHash = hashRefresh,
            ExpiresAt = rtExpiry,
        };

        stored.ReplacedByTokenId = newToken.Id;

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(accessToken, accessExpiry, rawRefresh, rtExpiry);
    }
}
