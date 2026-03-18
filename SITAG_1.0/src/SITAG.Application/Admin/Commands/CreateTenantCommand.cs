using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Admin.Commands;

/// <summary>
/// Atomically creates a Tenant + Producer + initial owner User (REQ-ONBOARDING-01).
/// The initial user always has MustChangePassword = true.
/// </summary>
public sealed record CreateTenantCommand(
    string Name,
    string PrimaryEmail,
    string? Notes,
    string OwnerFirstName,
    string OwnerLastName,
    string? OwnerPhone,
    string OwnerPassword) : IRequest<TenantDetailDto>;

public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher       _hasher;

    public CreateTenantCommandHandler(IApplicationDbContext db, IPasswordHasher hasher)
    {
        _db     = db;
        _hasher = hasher;
    }

    public async Task<TenantDetailDto> Handle(CreateTenantCommand req, CancellationToken ct)
    {
        var email = req.PrimaryEmail.ToLowerInvariant();

        if (await _db.Tenants.AnyAsync(t => t.PrimaryEmail == email, ct))
            throw new InvalidOperationException("A tenant with this primary email already exists.");

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("A user with this email already exists.");

        var tenant = new Tenant
        {
            Name         = req.Name.Trim(),
            PrimaryEmail = email,
            Notes        = req.Notes?.Trim(),
            Status       = TenantStatus.Active,
        };

        var producer = new Producer
        {
            TenantId    = tenant.Id,
            DisplayName = req.Name.Trim(),
        };

        var ownerUser = new User
        {
            TenantId           = tenant.Id,
            Email              = email,
            PasswordHash       = _hasher.Hash(req.OwnerPassword),
            Role               = UserRole.Productor,
            IsActive           = true,
            MustChangePassword = true,
            FirstName          = req.OwnerFirstName.Trim(),
            LastName           = req.OwnerLastName.Trim(),
            Phone              = req.OwnerPhone?.Trim(),
        };

        _db.Tenants.Add(tenant);
        _db.Producers.Add(producer);
        _db.Users.Add(ownerUser);
        await _db.SaveChangesAsync(ct);

        return new TenantDetailDto(
            tenant.Id, tenant.Name, tenant.PrimaryEmail,
            tenant.Status, tenant.PaidUntil, tenant.Notes,
            tenant.CreatedAt, UserCount: 1);
    }
}
