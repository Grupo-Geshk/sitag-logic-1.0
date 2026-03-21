using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Admin.Commands;

/// <summary>
/// Creates a new user under an existing tenant (REQ-USER-01).
/// The user is created with MustChangePassword = true.
/// </summary>
public sealed record CreateUserCommand(
    Guid TenantId,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    UserRole Role,
    string Password) : IRequest<UserDto>;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher       _hasher;

    public CreateUserCommandHandler(IApplicationDbContext db, IPasswordHasher hasher)
    {
        _db     = db;
        _hasher = hasher;
    }

    public async Task<UserDto> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == req.TenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant {req.TenantId} not found.");

        var email = req.Email.ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("A user with this email already exists.");

        var user = new User
        {
            TenantId           = req.TenantId,
            Email              = email,
            PasswordHash       = _hasher.Hash(req.Password),
            Role               = req.Role,
            IsActive           = true,
            MustChangePassword = true,
            FirstName          = req.FirstName.Trim(),
            LastName           = req.LastName.Trim(),
            Phone              = req.Phone?.Trim(),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new UserDto(
            user.Id, user.TenantId, tenant.Name,
            user.Email, user.FirstName, user.LastName, user.Phone,
            user.Role, user.IsActive, user.MustChangePassword,
            user.CreatedAt, user.LastLoginAt);
    }
}
