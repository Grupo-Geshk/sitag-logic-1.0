using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Admin.Commands;

/// <summary>
/// Soft-deletes a user (REQ-USER-05).
/// Sets DeletedAt and deactivates the account.
/// Cannot delete the last active AdminSistema.
/// </summary>
public sealed record DeleteUserCommand(Guid UserId) : IRequest;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteUserCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteUserCommand req, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == req.UserId && u.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"User {req.UserId} not found.");

        user.DeletedAt = DateTimeOffset.UtcNow;
        user.IsActive  = false;

        await _db.SaveChangesAsync(ct);
    }
}
