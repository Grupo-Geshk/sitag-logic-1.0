using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Enums;

namespace SITAG.Application.Admin.Commands;

/// <summary>
/// Edits a user's profile fields (REQ-USER-03).
/// Email and Role updates are allowed; they are intentional admin actions.
/// </summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? Phone,
    string Email,
    UserRole Role,
    bool IsActive) : IRequest<UserDto>;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _db;

    public UpdateUserCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<UserDto> Handle(UpdateUserCommand req, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == req.UserId, ct)
            ?? throw new KeyNotFoundException($"User {req.UserId} not found.");

        var newEmail = req.Email.ToLowerInvariant();

        if (user.Email != newEmail && await _db.Users.AnyAsync(u => u.Email == newEmail && u.Id != req.UserId, ct))
            throw new InvalidOperationException("A user with this email already exists.");

        user.FirstName = req.FirstName.Trim();
        user.LastName  = req.LastName.Trim();
        user.Phone     = req.Phone?.Trim();
        user.Email     = newEmail;
        user.Role      = req.Role;
        user.IsActive  = req.IsActive;

        await _db.SaveChangesAsync(ct);

        return new UserDto(
            user.Id, user.TenantId, user.Tenant.Name,
            user.Email, user.FirstName, user.LastName, user.Phone,
            user.Role, user.IsActive, user.MustChangePassword,
            user.CreatedAt, user.LastLoginAt);
    }
}
