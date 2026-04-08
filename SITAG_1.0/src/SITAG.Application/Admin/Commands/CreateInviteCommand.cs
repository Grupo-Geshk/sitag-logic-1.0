using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Common.Plans;
using SITAG.Domain.Common;
using SITAG.Domain.Entities;

namespace SITAG.Application.Admin.Commands;

public sealed record CreateInviteResult(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// Creates a single-use invite token for a new user to join an existing tenant.
/// Validates plan user limits before issuing the invite.
/// The raw token is returned once — it is never stored.
/// The caller (frontend) is responsible for building the full invite URL.
/// </summary>
public sealed record CreateInviteCommand(
    Guid TenantId,
    string Email,
    Guid ActorUserId) : IRequest<CreateInviteResult>;

public sealed class CreateInviteCommandHandler : IRequestHandler<CreateInviteCommand, CreateInviteResult>
{
    private readonly IApplicationDbContext _db;

    public CreateInviteCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CreateInviteResult> Handle(CreateInviteCommand req, CancellationToken ct)
    {
        var email = req.Email.ToLowerInvariant();

        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == req.TenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant {req.TenantId} not found.");

        // Check plan user limit
        var currentUsers = await _db.Users
            .CountAsync(u => u.TenantId == req.TenantId && u.IsActive && u.DeletedAt == null, ct);

        var maxUsers = PlanLimits.MaxUsers(tenant.Plan);
        if (maxUsers != PlanLimits.Unlimited && currentUsers >= maxUsers)
            throw new InvalidOperationException(
                $"El plan {tenant.Plan} permite un máximo de {maxUsers} usuario(s). " +
                "Actualice el plan para agregar más usuarios.");

        // Prevent duplicate active invite for same email in this tenant
        var existingInvite = await _db.UserInvites
            .FirstOrDefaultAsync(i =>
                i.TenantId == req.TenantId &&
                i.Email    == email        &&
                i.AcceptedAt == null       &&
                i.ExpiresAt > DateTimeOffset.UtcNow, ct);

        if (existingInvite is not null)
        {
            // Expire it so admin can issue a fresh one
            existingInvite.ExpiresAt = DateTimeOffset.UtcNow;
        }

        // Prevent inviting an email that already has an active user in this tenant
        var userExists = await _db.Users
            .AnyAsync(u => u.TenantId == req.TenantId && u.Email == email && u.DeletedAt == null, ct);

        if (userExists)
            throw new ConflictException("Ya existe un usuario con ese correo en este tenant.");

        var raw       = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        var expiresAt = DateTimeOffset.UtcNow.AddHours(72);

        _db.UserInvites.Add(new UserInvite
        {
            TenantId        = req.TenantId,
            Email           = email,
            TokenHash       = tokenHash,
            ExpiresAt       = expiresAt,
            CreatedByUserId = req.ActorUserId,
        });

        await _db.SaveChangesAsync(ct);

        // Return the URL-safe raw token — the frontend builds the full URL
        var urlToken = Uri.EscapeDataString(raw);

        return new CreateInviteResult(urlToken, expiresAt);
    }
}
