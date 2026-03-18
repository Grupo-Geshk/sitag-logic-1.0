using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Admin.Queries;

public sealed record GetTenantAuditLogQuery(Guid TenantId) : IRequest<IReadOnlyList<TenantAuditEntryDto>>;

public sealed class GetTenantAuditLogQueryHandler
    : IRequestHandler<GetTenantAuditLogQuery, IReadOnlyList<TenantAuditEntryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetTenantAuditLogQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<TenantAuditEntryDto>> Handle(
        GetTenantAuditLogQuery req, CancellationToken ct)
    {
        var exists = await _db.Tenants.AnyAsync(t => t.Id == req.TenantId, ct);
        if (!exists) throw new KeyNotFoundException($"Tenant {req.TenantId} not found.");

        return await _db.TenantAuditLogs
            .AsNoTracking()
            .Where(l => l.TenantId == req.TenantId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new TenantAuditEntryDto(
                l.Id,
                l.ActorUserId,
                l.ActorEmail,
                l.Action.ToString(),
                l.FromStatus.HasValue ? l.FromStatus.ToString() : null,
                l.ToStatus.HasValue   ? l.ToStatus.ToString()   : null,
                l.PaidUntil,
                l.Note,
                l.CreatedAt))
            .ToListAsync(ct);
    }
}
