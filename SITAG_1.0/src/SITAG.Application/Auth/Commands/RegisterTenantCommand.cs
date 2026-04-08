using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Auth.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Common;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Auth.Commands;

/// <summary>
/// Public self-registration: creates a new Tenant + Producer + owner User in one atomic operation.
/// The tenant starts on Plan=Semilla (free tier) with Status=Active and no PaidUntil.
/// Returns a token pair so the user is logged in immediately after registration.
/// </summary>
public sealed record RegisterTenantCommand(
    string TenantName,
    string FirstName,
    string LastName,
    string? Phone,
    string Email,
    string Password) : IRequest<AuthTokensDto>;

public sealed class RegisterTenantCommandHandler : IRequestHandler<RegisterTenantCommand, AuthTokensDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher       _hasher;
    private readonly ITokenService         _tokens;

    public RegisterTenantCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher       hasher,
        ITokenService         tokens)
    {
        _db     = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthTokensDto> Handle(RegisterTenantCommand req, CancellationToken ct)
    {
        var email = req.Email.ToLowerInvariant();

        if (await _db.Tenants.AnyAsync(t => t.PrimaryEmail == email, ct))
            throw new ConflictException("A tenant with this email already exists.");

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("A user with this email already exists.");

        var tenant = new Tenant
        {
            Name         = req.TenantName.Trim(),
            PrimaryEmail = email,
            Status       = TenantStatus.Active,
            Plan         = TenantPlan.Semilla,
            PaidUntil    = null,
        };

        var producer = new Producer
        {
            TenantId    = tenant.Id,
            DisplayName = req.TenantName.Trim(),
        };

        var owner = new User
        {
            TenantId           = tenant.Id,
            Email              = email,
            PasswordHash       = _hasher.Hash(req.Password),
            Role               = UserRole.Productor,
            IsActive           = true,
            MustChangePassword = false,
            FirstName          = req.FirstName.Trim(),
            LastName           = req.LastName.Trim(),
            Phone              = req.Phone?.Trim(),
        };

        _db.Tenants.Add(tenant);
        _db.Producers.Add(producer);
        _db.Users.Add(owner);
        await _db.SaveChangesAsync(ct);

        var (accessToken, accessExpiry)         = _tokens.GenerateAccessToken(owner, TenantPlan.Semilla);
        var (rawRefresh, hashRefresh, rtExpiry) = _tokens.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId    = owner.Id,
            TenantId  = tenant.Id,
            TokenHash = hashRefresh,
            ExpiresAt = rtExpiry,
        });

        await _db.SaveChangesAsync(ct);

        return new AuthTokensDto(accessToken, accessExpiry, rawRefresh, rtExpiry, MustChangePassword: false);
    }
}
