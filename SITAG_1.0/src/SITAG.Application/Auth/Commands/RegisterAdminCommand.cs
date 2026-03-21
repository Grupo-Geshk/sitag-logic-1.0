using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Auth.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Common;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Auth.Commands;

/// <summary>
/// Bootstrap endpoint: creates the first AdminSistema user together with its Tenant.
/// Fails with InvalidOperationException if any AdminSistema already exists.
/// </summary>
public sealed record RegisterAdminCommand(
    string TenantName,
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<AuthTokensDto>;

public sealed class RegisterAdminCommandHandler : IRequestHandler<RegisterAdminCommand, AuthTokensDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher       _hasher;
    private readonly ITokenService         _tokens;

    public RegisterAdminCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher       hasher,
        ITokenService         tokens)
    {
        _db     = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthTokensDto> Handle(RegisterAdminCommand req, CancellationToken ct)
    {
        var adminExists = await _db.Users
            .AnyAsync(u => u.Role == UserRole.AdminSistema, ct);

        if (adminExists)
            throw new ConflictException(
                "An admin account already exists. Use the admin panel to manage users.");

        var tenant = new Tenant
        {
            Name         = req.TenantName.Trim(),
            PrimaryEmail = req.Email.ToLowerInvariant(),
            Status       = TenantStatus.Active,
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct); // get tenant.Id

        var user = new User
        {
            TenantId           = tenant.Id,
            Email              = req.Email.ToLowerInvariant(),
            PasswordHash       = _hasher.Hash(req.Password),
            Role               = UserRole.AdminSistema,
            IsActive           = true,
            MustChangePassword = false,
            FirstName          = req.FirstName.Trim(),
            LastName           = req.LastName.Trim(),
        };

        _db.Users.Add(user);

        var (rawRefresh, hashRefresh, rtExpiry) = _tokens.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId    = user.Id,
            TenantId  = tenant.Id,
            TokenHash = hashRefresh,
            ExpiresAt = rtExpiry,
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        // Admin users always get Corporativo plan (no tenant plan applies)
        var (accessToken, accessExpiry) = _tokens.GenerateAccessToken(user, Domain.Enums.TenantPlan.Corporativo);

        return new AuthTokensDto(accessToken, accessExpiry, rawRefresh, rtExpiry, MustChangePassword: false);
    }
}
