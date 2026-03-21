using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Auth.Dtos;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthTokensDto>;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokensDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher       _hasher;
    private readonly ITokenService         _tokens;

    public LoginCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher       hasher,
        ITokenService         tokens)
    {
        _db     = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthTokensDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account has been deactivated.");

        user.LastLoginAt = DateTimeOffset.UtcNow;

        var (accessToken, accessExpiry)          = _tokens.GenerateAccessToken(user);
        var (rawRefresh, hashRefresh, rtExpiry)  = _tokens.GenerateRefreshToken();

        var refreshToken = new Domain.Entities.RefreshToken
        {
            UserId    = user.Id,
            TenantId  = user.TenantId,
            TokenHash = hashRefresh,
            ExpiresAt = rtExpiry,
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(accessToken, accessExpiry, rawRefresh, rtExpiry, user.MustChangePassword);
    }
}
