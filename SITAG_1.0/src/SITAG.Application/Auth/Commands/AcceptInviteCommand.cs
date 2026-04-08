using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Auth.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Auth.Commands;

/// <summary>
/// Consumes a valid invite token: creates the user in the tenant and returns
/// a token pair so the user is logged in immediately after completing registration.
/// </summary>
public sealed record AcceptInviteCommand(
    string RawToken,
    string FirstName,
    string LastName,
    string? Phone,
    string Password) : IRequest<AuthTokensDto>;

public sealed class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, AuthTokensDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher       _hasher;
    private readonly ITokenService         _tokens;

    public AcceptInviteCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher       hasher,
        ITokenService         tokens)
    {
        _db     = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthTokensDto> Handle(AcceptInviteCommand req, CancellationToken ct)
    {
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(req.RawToken)));

        var invite = await _db.UserInvites
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.TokenHash == hash, ct)
            ?? throw new KeyNotFoundException("El enlace de invitación no es válido.");

        if (!invite.IsValid)
            throw new InvalidOperationException(
                invite.IsExpired  ? "El enlace de invitación ha expirado." :
                invite.IsAccepted ? "Este enlace de invitación ya fue utilizado." :
                                    "El enlace de invitación no es válido.");

        // Double-check email not taken (race condition guard)
        var emailTaken = await _db.Users
            .AnyAsync(u => u.TenantId == invite.TenantId && u.Email == invite.Email && u.DeletedAt == null, ct);

        if (emailTaken)
            throw new InvalidOperationException("Ya existe un usuario con este correo en el tenant.");

        var user = new User
        {
            TenantId           = invite.TenantId,
            Email              = invite.Email,
            PasswordHash       = _hasher.Hash(req.Password),
            Role               = UserRole.Productor,
            IsActive           = true,
            MustChangePassword = false,
            FirstName          = req.FirstName.Trim(),
            LastName           = req.LastName.Trim(),
            Phone              = req.Phone?.Trim(),
        };

        // Consume the invite
        invite.AcceptedAt      = DateTimeOffset.UtcNow;
        invite.AcceptedByUserId = user.Id;

        _db.Users.Add(user);

        var plan                                    = invite.Tenant.Plan;
        var (accessToken, accessExpiry)             = _tokens.GenerateAccessToken(user, plan);
        var (rawRefresh, hashRefresh, rtExpiry)     = _tokens.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId    = user.Id,
            TenantId  = invite.TenantId,
            TokenHash = hashRefresh,
            ExpiresAt = rtExpiry,
        });

        await _db.SaveChangesAsync(ct);

        return new AuthTokensDto(accessToken, accessExpiry, rawRefresh, rtExpiry, MustChangePassword: false);
    }
}
