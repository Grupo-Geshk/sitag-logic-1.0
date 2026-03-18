using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Supplies.Commands;
using SITAG.Application.Supplies.Dtos;

namespace SITAG.Application.Supplies.Queries;

public sealed record GetSuppliesQuery(Guid? FarmId, bool? LowStockOnly) : IRequest<IReadOnlyList<SupplyDto>>;

public sealed class GetSuppliesHandler : IRequestHandler<GetSuppliesQuery, IReadOnlyList<SupplyDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetSuppliesHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<SupplyDto>> Handle(GetSuppliesQuery r, CancellationToken ct)
    {
        var query = _db.Supplies.AsNoTracking()
            .Where(s => s.TenantId == _user.TenantId && s.DeletedAt == null);

        if (r.FarmId.HasValue) query = query.Where(s => s.FarmId == r.FarmId);
        if (r.LowStockOnly == true) query = query.Where(s => s.CurrentQuantity <= s.MinStockLevel);

        return await query
            .OrderBy(s => s.Name)
            .Select(s => new SupplyDto(s.Id, s.TenantId, s.FarmId, s.Name, s.Category, s.Unit,
                s.CurrentQuantity, s.MinStockLevel, s.ExpirationDate,
                s.CurrentQuantity <= s.MinStockLevel, s.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed record GetSupplyByIdQuery(Guid SupplyId) : IRequest<SupplyDto>;

public sealed class GetSupplyByIdHandler : IRequestHandler<GetSupplyByIdQuery, SupplyDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetSupplyByIdHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<SupplyDto> Handle(GetSupplyByIdQuery r, CancellationToken ct)
    {
        var s = await _db.Supplies.AsNoTracking()
            .Where(s => s.Id == r.SupplyId && s.TenantId == _user.TenantId && s.DeletedAt == null)
            .Select(s => new SupplyDto(s.Id, s.TenantId, s.FarmId, s.Name, s.Category, s.Unit,
                s.CurrentQuantity, s.MinStockLevel, s.ExpirationDate,
                s.CurrentQuantity <= s.MinStockLevel, s.CreatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Supply {r.SupplyId} not found.");
        return s;
    }
}

public sealed record GetSupplyMovementsQuery(Guid SupplyId) : IRequest<IReadOnlyList<SupplyMovementDto>>;

public sealed class GetSupplyMovementsHandler
    : IRequestHandler<GetSupplyMovementsQuery, IReadOnlyList<SupplyMovementDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetSupplyMovementsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<SupplyMovementDto>> Handle(GetSupplyMovementsQuery r, CancellationToken ct)
    {
        var exists = await _db.Supplies
            .AnyAsync(s => s.Id == r.SupplyId && s.TenantId == _user.TenantId, ct);
        if (!exists) throw new KeyNotFoundException($"Supply {r.SupplyId} not found.");

        return await _db.SupplyMovements.AsNoTracking()
            .Where(m => m.SupplyId == r.SupplyId && m.TenantId == _user.TenantId)
            .OrderByDescending(m => m.MovementDate)
            .Select(m => new SupplyMovementDto(m.Id, m.SupplyId, m.MovementType.ToString(),
                m.Quantity, m.PreviousQuantity, m.NewQuantity, m.Reason, m.MovementDate, m.CreatedAt))
            .ToListAsync(ct);
    }
}

// ── Supply alerts (low stock + expiring) ──────────────────────────────────────
public sealed record GetSupplyAlertsQuery(Guid? FarmId) : IRequest<IReadOnlyList<SupplyAlertDto>>;

public sealed class GetSupplyAlertsHandler : IRequestHandler<GetSupplyAlertsQuery, IReadOnlyList<SupplyAlertDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetSupplyAlertsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<SupplyAlertDto>> Handle(GetSupplyAlertsQuery r, CancellationToken ct)
    {
        var tid       = _user.TenantId;
        var expiryDay = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var alerts    = new List<SupplyAlertDto>();

        var query = _db.Supplies.AsNoTracking()
            .Where(s => s.TenantId == tid && s.DeletedAt == null);
        if (r.FarmId.HasValue) query = query.Where(s => s.FarmId == r.FarmId);

        var supplies = await query
            .Select(s => new
            {
                s.Id, s.Name, s.Unit, s.CurrentQuantity, s.MinStockLevel, s.ExpirationDate
            })
            .ToListAsync(ct);

        foreach (var s in supplies)
        {
            if (s.CurrentQuantity <= s.MinStockLevel)
                alerts.Add(new SupplyAlertDto(
                    s.Id, s.Name, "LOW_STOCK", "Media",
                    $"Insumo '{s.Name}' con stock bajo: {s.CurrentQuantity} {s.Unit} (mínimo {s.MinStockLevel}).",
                    s.CurrentQuantity, s.MinStockLevel, null));

            if (s.ExpirationDate.HasValue && s.ExpirationDate.Value <= expiryDay)
            {
                var days     = (s.ExpirationDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days;
                var severity = days <= 7 ? "Alta" : "Baja";
                var msg = days <= 0
                    ? $"Insumo '{s.Name}' ha vencido."
                    : $"Insumo '{s.Name}' vence en {days} día(s).";
                alerts.Add(new SupplyAlertDto(
                    s.Id, s.Name, "EXPIRING", severity, msg,
                    null, null, s.ExpirationDate));
            }
        }

        return alerts.OrderByDescending(a => a.Severity == "Alta" ? 2 : a.Severity == "Media" ? 1 : 0).ToList();
    }
}

// ── Service consumptions ──────────────────────────────────────────────────────
public sealed record GetServiceConsumptionsQuery(Guid ServiceId) : IRequest<IReadOnlyList<ServiceConsumptionDto>>;

public sealed class GetServiceConsumptionsHandler
    : IRequestHandler<GetServiceConsumptionsQuery, IReadOnlyList<ServiceConsumptionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetServiceConsumptionsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<ServiceConsumptionDto>> Handle(GetServiceConsumptionsQuery r, CancellationToken ct)
    {
        var exists = await _db.VetServices
            .AnyAsync(s => s.Id == r.ServiceId && s.TenantId == _user.TenantId, ct);
        if (!exists) throw new KeyNotFoundException($"Service {r.ServiceId} not found.");

        return await _db.ServiceSupplyConsumptions.AsNoTracking()
            .Where(c => c.ServiceId == r.ServiceId && c.TenantId == _user.TenantId)
            .Select(c => new ServiceConsumptionDto(
                c.Id, c.ServiceId, c.SupplyId,
                c.Supply.Name, c.Supply.Unit, c.Quantity, c.CreatedAt))
            .ToListAsync(ct);
    }
}

// ── Supply usage report (REQ-SUPPLY-04) ──────────────────────────────────────
public sealed record GetSupplyUsageQuery(
    Guid?           FarmId,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate) : IRequest<IReadOnlyList<SupplyUsageDto>>;

public sealed class GetSupplyUsageHandler
    : IRequestHandler<GetSupplyUsageQuery, IReadOnlyList<SupplyUsageDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetSupplyUsageHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<SupplyUsageDto>> Handle(GetSupplyUsageQuery r, CancellationToken ct)
    {
        var tid = _user.TenantId;

        var query = _db.SupplyMovements.AsNoTracking()
            .Where(m => m.TenantId == tid
                     && m.MovementType == Domain.Enums.SupplyMovementType.Consumption);

        if (r.StartDate.HasValue) query = query.Where(m => m.MovementDate >= r.StartDate);
        if (r.EndDate.HasValue)   query = query.Where(m => m.MovementDate <= r.EndDate);

        if (r.FarmId.HasValue)
        {
            var supplyIds = await _db.Supplies.AsNoTracking()
                .Where(s => s.TenantId == tid && s.FarmId == r.FarmId)
                .Select(s => s.Id)
                .ToListAsync(ct);
            query = query.Where(m => supplyIds.Contains(m.SupplyId));
        }

        var movements = await query
            .Select(m => new { m.SupplyId, m.Quantity })
            .ToListAsync(ct);

        if (!movements.Any()) return Array.Empty<SupplyUsageDto>();

        var supplyIdsUsed = movements.Select(m => m.SupplyId).Distinct().ToList();
        var supplyNames = await _db.Supplies.AsNoTracking()
            .Where(s => supplyIdsUsed.Contains(s.Id) && s.TenantId == tid)
            .Select(s => new { s.Id, s.Name, s.Unit })
            .ToDictionaryAsync(s => s.Id, ct);

        return movements
            .GroupBy(m => m.SupplyId)
            .Select(g =>
            {
                var info = supplyNames.GetValueOrDefault(g.Key);
                return new SupplyUsageDto(
                    g.Key,
                    info?.Name ?? "—",
                    info?.Unit ?? "—",
                    g.Sum(m => m.Quantity),
                    g.Count());
            })
            .OrderByDescending(u => u.TotalConsumed)
            .ToList();
    }
}
