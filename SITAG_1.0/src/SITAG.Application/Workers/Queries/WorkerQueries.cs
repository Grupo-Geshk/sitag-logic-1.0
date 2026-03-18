using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Workers.Dtos;
using SITAG.Domain.Enums;

namespace SITAG.Application.Workers.Queries;

// ── List ──────────────────────────────────────────────────────────────────────
public sealed record GetWorkersQuery(
    string? Search, WorkerStatus? Status, Guid? FarmId,
    int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<WorkerDto>>;

public sealed class GetWorkersHandler : IRequestHandler<GetWorkersQuery, PagedResult<WorkerDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetWorkersHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<PagedResult<WorkerDto>> Handle(GetWorkersQuery r, CancellationToken ct)
    {
        var tid   = _user.TenantId;
        var query = _db.Workers.AsNoTracking()
            .Where(w => w.TenantId == tid && w.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var s = r.Search.Trim().ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(s));
        }
        if (r.Status.HasValue) query = query.Where(w => w.Status == r.Status);
        if (r.FarmId.HasValue)
            query = query.Where(w => w.FarmAssignments.Any(a => a.FarmId == r.FarmId && a.EndDate == null));

        var total = await query.CountAsync(ct);

        var workers = await query
            .OrderBy(w => w.Name)
            .Skip((r.PageNumber - 1) * r.PageSize)
            .Take(r.PageSize)
            .Select(w => new
            {
                w.Id, w.TenantId, w.Name, w.RoleLabel, w.Contact, w.Status, w.CreatedAt,
                FarmIds = w.FarmAssignments.Where(a => a.EndDate == null).Select(a => a.FarmId).ToList()
            })
            .ToListAsync(ct);

        var items = workers
            .Select(w => new WorkerDto(w.Id, w.TenantId, w.Name, w.RoleLabel, w.Contact, w.Status, w.CreatedAt, w.FarmIds))
            .ToList();

        return new PagedResult<WorkerDto>(items, total, r.PageNumber, r.PageSize);
    }
}

// ── By ID ─────────────────────────────────────────────────────────────────────
public sealed record GetWorkerByIdQuery(Guid WorkerId) : IRequest<WorkerDto>;

public sealed class GetWorkerByIdHandler : IRequestHandler<GetWorkerByIdQuery, WorkerDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetWorkerByIdHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<WorkerDto> Handle(GetWorkerByIdQuery r, CancellationToken ct)
    {
        var w = await _db.Workers.AsNoTracking()
            .Where(w => w.Id == r.WorkerId && w.TenantId == _user.TenantId && w.DeletedAt == null)
            .Select(w => new
            {
                w.Id, w.TenantId, w.Name, w.RoleLabel, w.Contact, w.Status, w.CreatedAt,
                FarmIds = w.FarmAssignments.Where(a => a.EndDate == null).Select(a => a.FarmId).ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Worker {r.WorkerId} not found.");

        return new WorkerDto(w.Id, w.TenantId, w.Name, w.RoleLabel, w.Contact, w.Status, w.CreatedAt, w.FarmIds);
    }
}

// ── Assignment history ────────────────────────────────────────────────────────
public sealed record GetWorkerAssignmentHistoryQuery(Guid WorkerId) : IRequest<IReadOnlyList<WorkerAssignmentDto>>;

public sealed class GetWorkerAssignmentHistoryHandler
    : IRequestHandler<GetWorkerAssignmentHistoryQuery, IReadOnlyList<WorkerAssignmentDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetWorkerAssignmentHistoryHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<WorkerAssignmentDto>> Handle(
        GetWorkerAssignmentHistoryQuery r, CancellationToken ct)
    {
        var exists = await _db.Workers
            .AnyAsync(w => w.Id == r.WorkerId && w.TenantId == _user.TenantId, ct);
        if (!exists) throw new KeyNotFoundException($"Worker {r.WorkerId} not found.");

        return await _db.WorkerFarmAssignments
            .AsNoTracking()
            .Where(a => a.WorkerId == r.WorkerId && a.TenantId == _user.TenantId)
            .OrderByDescending(a => a.StartDate)
            .Select(a => new WorkerAssignmentDto(a.Id, a.WorkerId, a.FarmId, a.StartDate, a.EndDate))
            .ToListAsync(ct);
    }
}

// ── Payment history ───────────────────────────────────────────────────────────
public sealed record GetWorkerPaymentsQuery(Guid WorkerId) : IRequest<IReadOnlyList<WorkerPaymentDto>>;

public sealed class GetWorkerPaymentsHandler
    : IRequestHandler<GetWorkerPaymentsQuery, IReadOnlyList<WorkerPaymentDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetWorkerPaymentsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<WorkerPaymentDto>> Handle(GetWorkerPaymentsQuery r, CancellationToken ct)
    {
        var tid = _user.TenantId;

        var exists = await _db.Workers
            .AnyAsync(w => w.Id == r.WorkerId && w.TenantId == tid && w.DeletedAt == null, ct);
        if (!exists) throw new KeyNotFoundException($"Worker {r.WorkerId} not found.");

        return await _db.WorkerPayments
            .AsNoTracking()
            .Where(p => p.WorkerId == r.WorkerId && p.TenantId == tid)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new WorkerPaymentDto(
                p.Id, p.WorkerId, p.Mode, p.PaymentDate,
                p.Rate, p.Quantity, p.TotalAmount,
                p.Notes, p.FarmId, p.TransactionId, p.CreatedAt))
            .ToListAsync(ct);
    }
}

// ── Activity timeline ─────────────────────────────────────────────────────────
public sealed record GetWorkerActivityTimelineQuery(Guid WorkerId) : IRequest<WorkerActivityTimelineDto>;

public sealed class GetWorkerActivityTimelineHandler
    : IRequestHandler<GetWorkerActivityTimelineQuery, WorkerActivityTimelineDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetWorkerActivityTimelineHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<WorkerActivityTimelineDto> Handle(GetWorkerActivityTimelineQuery r, CancellationToken ct)
    {
        var tid = _user.TenantId;

        var exists = await _db.Workers
            .AnyAsync(w => w.Id == r.WorkerId && w.TenantId == tid && w.DeletedAt == null, ct);
        if (!exists) throw new KeyNotFoundException($"Worker {r.WorkerId} not found.");

        var events = await _db.AnimalEvents
            .AsNoTracking()
            .Where(e => e.WorkerId == r.WorkerId && e.TenantId == tid)
            .Select(e => new WorkerActivityEntryDto(
                "EVENT",
                e.EventDate,
                $"{e.EventType} — Animal {e.AnimalId}",
                e.AnimalId,
                null,
                e.Cost))
            .ToListAsync(ct);

        var services = await _db.VetServices
            .AsNoTracking()
            .Where(s => s.WorkerId == r.WorkerId && s.TenantId == tid)
            .Select(s => new WorkerActivityEntryDto(
                "SERVICE",
                s.ScheduledDate,
                $"{s.ServiceType} [{s.Status}]",
                null,
                s.Id,
                s.Cost))
            .ToListAsync(ct);

        var timeline = events.Concat(services)
            .OrderByDescending(e => e.Date)
            .ToList();

        return new WorkerActivityTimelineDto(r.WorkerId, timeline);
    }
}
