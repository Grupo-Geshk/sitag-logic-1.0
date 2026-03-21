using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Admin.Commands;

/// <summary>
/// Updates a tenant's subscription status and/or paidUntil date,
/// and writes an immutable audit log entry (REQ-TENANT-03, REQ-TENANT-05, REQ-TENANT-06).
/// </summary>
public sealed record UpdateTenantStatusCommand(
    Guid TenantId,
    TenantStatus Status,
    DateTimeOffset? PaidUntil,
    string? Note,
    // Set by the controller from the current user's JWT claims
    Guid ActorUserId,
    string ActorEmail) : IRequest;

public sealed class UpdateTenantStatusCommandHandler : IRequestHandler<UpdateTenantStatusCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateTenantStatusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateTenantStatusCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == req.TenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant {req.TenantId} not found.");

        var fromStatus = tenant.Status;

        tenant.Status   = req.Status;
        tenant.PaidUntil = req.PaidUntil ?? tenant.PaidUntil;

        var logEntry = new TenantAuditLog
        {
            TenantId     = tenant.Id,
            ActorUserId  = req.ActorUserId,
            ActorEmail   = req.ActorEmail,
            Action       = TenantAuditAction.StatusChange,
            FromStatus   = fromStatus,
            ToStatus     = req.Status,
            PaidUntil    = req.PaidUntil,
            Note         = req.Note?.Trim(),
        };

        _db.TenantAuditLogs.Add(logEntry);

        // Revoke all active refresh tokens for this tenant when status becomes Delinquent.
        // Access tokens remain valid up to 15 min (stateless), but users cannot renew them.
        if (req.Status == TenantStatus.Delinquent && fromStatus != TenantStatus.Delinquent)
        {
            var now = DateTimeOffset.UtcNow;
            var activeTokens = await _db.RefreshTokens
                .Where(rt => rt.TenantId == req.TenantId && rt.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var rt in activeTokens)
                rt.RevokedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }
}
