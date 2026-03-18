using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Admin.Commands;

/// <summary>
/// Activates or deactivates a user (REQ-USER-04).
/// </summary>
public sealed record SetUserActiveCommand(Guid UserId, bool IsActive) : IRequest;

public sealed class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly IApplicationDbContext _db;

    public SetUserActiveCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(SetUserActiveCommand req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId, ct)
            ?? throw new KeyNotFoundException($"User {req.UserId} not found.");

        user.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
    }
}
