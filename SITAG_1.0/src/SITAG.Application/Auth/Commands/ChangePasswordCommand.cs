using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Auth.Commands;

/// <summary>
/// Changes the authenticated user's password (REQ-ONBOARDING-02).
/// Clears the MustChangePassword flag on success.
/// </summary>
public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser          _currentUser;
    private readonly IPasswordHasher       _hasher;

    public ChangePasswordCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IPasswordHasher hasher)
    {
        _db          = db;
        _currentUser = currentUser;
        _hasher      = hasher;
    }

    public async Task Handle(ChangePasswordCommand req, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!_hasher.Verify(req.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        if (req.NewPassword.Length < 8)
            throw new ArgumentException("New password must be at least 8 characters.");

        user.PasswordHash      = _hasher.Hash(req.NewPassword);
        user.MustChangePassword = false;

        await _db.SaveChangesAsync(ct);
    }
}
