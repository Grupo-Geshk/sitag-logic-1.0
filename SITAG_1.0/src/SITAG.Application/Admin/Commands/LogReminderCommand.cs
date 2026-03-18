using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Admin.Commands;

/// <summary>
/// Records that the admin manually sent a payment reminder (REQ-TENANT-08).
/// No actual email is sent in the MVP; just an audit log entry.
/// </summary>
public sealed record LogReminderCommand(
    Guid TenantId,
    string? Note,
    Guid ActorUserId,
    string ActorEmail) : IRequest;

public sealed class LogReminderCommandHandler : IRequestHandler<LogReminderCommand>
{
    private readonly IApplicationDbContext _db;

    public LogReminderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(LogReminderCommand req, CancellationToken ct)
    {
        var exists = await _db.Tenants.AnyAsync(t => t.Id == req.TenantId, ct);
        if (!exists)
            throw new KeyNotFoundException($"Tenant {req.TenantId} not found.");

        var logEntry = new TenantAuditLog
        {
            TenantId    = req.TenantId,
            ActorUserId = req.ActorUserId,
            ActorEmail  = req.ActorEmail,
            Action      = TenantAuditAction.ReminderSent,
            Note        = req.Note?.Trim(),
        };

        _db.TenantAuditLogs.Add(logEntry);
        await _db.SaveChangesAsync(ct);
    }
}
