using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Farms.Dtos;
using SITAG.Domain.Enums;

namespace SITAG.Application.Farms.Queries;

// ── List ─────────────────────────────────────────────────────────────────────
public sealed record GetFarmsQuery : IRequest<IReadOnlyList<FarmDto>>;

public sealed class GetFarmsHandler : IRequestHandler<GetFarmsQuery, IReadOnlyList<FarmDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetFarmsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<FarmDto>> Handle(GetFarmsQuery _, CancellationToken ct) =>
        await _db.Farms
            .AsNoTracking()
            .Where(f => f.TenantId == _user.TenantId && f.DeletedAt == null)
            .OrderBy(f => f.Name)
            .Select(f => new FarmDto(f.Id, f.TenantId, f.Name, f.Location, f.Hectares, f.FarmType, f.IsOwned, f.CreatedAt))
            .ToListAsync(ct);
}

// ── By ID ────────────────────────────────────────────────────────────────────
public sealed record GetFarmByIdQuery(Guid FarmId) : IRequest<FarmDto>;

public sealed class GetFarmByIdHandler : IRequestHandler<GetFarmByIdQuery, FarmDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetFarmByIdHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<FarmDto> Handle(GetFarmByIdQuery r, CancellationToken ct)
    {
        var f = await _db.Farms
            .AsNoTracking()
            .Where(f => f.Id == r.FarmId && f.TenantId == _user.TenantId && f.DeletedAt == null)
            .Select(f => new FarmDto(f.Id, f.TenantId, f.Name, f.Location, f.Hectares, f.FarmType, f.IsOwned, f.CreatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Farm {r.FarmId} not found.");
        return f;
    }
}

// ── Overview (summary cards) ─────────────────────────────────────────────────
public sealed record GetFarmsOverviewQuery : IRequest<FarmsOverviewDto>;

public sealed class GetFarmsOverviewHandler : IRequestHandler<GetFarmsOverviewQuery, FarmsOverviewDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetFarmsOverviewHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<FarmsOverviewDto> Handle(GetFarmsOverviewQuery _, CancellationToken ct)
    {
        var tid = _user.TenantId;

        var farms = await _db.Farms
            .AsNoTracking()
            .Where(f => f.TenantId == tid && f.DeletedAt == null)
            .Select(f => new FarmDetailDto(
                f.Id, f.TenantId, f.Name, f.Location, f.Hectares, f.FarmType, f.IsOwned, f.CreatedAt,
                f.Animals.Count(a => a.Status == AnimalStatus.Activo),
                f.Animals.Count(a => a.Status == AnimalStatus.Activo &&
                    (a.HealthStatus == AnimalHealthStatus.Enfermo || a.HealthStatus == AnimalHealthStatus.Critico)),
                f.Divisions.Count(d => d.DeletedAt == null),
                0)) // worker count omitted for perf in overview
            .ToListAsync(ct);

        return new FarmsOverviewDto(farms,
            TotalFarms: farms.Count,
            TotalActiveAnimals: farms.Sum(f => f.ActiveAnimals),
            TotalSickAnimals: farms.Sum(f => f.SickAnimals));
    }
}

// ── Detail (single farm expanded) ────────────────────────────────────────────
public sealed record GetFarmDetailQuery(Guid FarmId) : IRequest<FarmDetailDto>;

public sealed class GetFarmDetailHandler : IRequestHandler<GetFarmDetailQuery, FarmDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetFarmDetailHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<FarmDetailDto> Handle(GetFarmDetailQuery r, CancellationToken ct)
    {
        var tid = _user.TenantId;
        var f = await _db.Farms
            .AsNoTracking()
            .Where(f => f.Id == r.FarmId && f.TenantId == tid && f.DeletedAt == null)
            .Select(f => new FarmDetailDto(
                f.Id, f.TenantId, f.Name, f.Location, f.Hectares, f.FarmType, f.IsOwned, f.CreatedAt,
                f.Animals.Count(a => a.Status == AnimalStatus.Activo),
                f.Animals.Count(a => a.Status == AnimalStatus.Activo &&
                    (a.HealthStatus == AnimalHealthStatus.Enfermo || a.HealthStatus == AnimalHealthStatus.Critico)),
                f.Divisions.Count(d => d.DeletedAt == null),
                _db.WorkerFarmAssignments.Count(wfa => wfa.FarmId == f.Id && wfa.EndDate == null)))
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Farm {r.FarmId} not found.");
        return f;
    }
}

// ── Division by ID ────────────────────────────────────────────────────────────
public sealed record GetDivisionByIdQuery(Guid DivisionId) : IRequest<DivisionDto>;

public sealed class GetDivisionByIdHandler : IRequestHandler<GetDivisionByIdQuery, DivisionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetDivisionByIdHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<DivisionDto> Handle(GetDivisionByIdQuery r, CancellationToken ct)
    {
        var d = await _db.Divisions
            .AsNoTracking()
            .Where(d => d.Id == r.DivisionId && d.TenantId == _user.TenantId && d.DeletedAt == null)
            .Select(d => new DivisionDto(d.Id, d.FarmId, d.Name, d.MaxCapacity, d.IsActive, d.CreatedAt,
                _db.Animals.Count(a => a.DivisionId == d.Id && a.Status == AnimalStatus.Activo)))
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Division {r.DivisionId} not found.");
        return d;
    }
}

// ── Divisions list ────────────────────────────────────────────────────────────
public sealed record GetDivisionsQuery(Guid FarmId) : IRequest<IReadOnlyList<DivisionDto>>;

public sealed class GetDivisionsHandler : IRequestHandler<GetDivisionsQuery, IReadOnlyList<DivisionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetDivisionsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<DivisionDto>> Handle(GetDivisionsQuery r, CancellationToken ct) =>
        await _db.Divisions
            .AsNoTracking()
            .Where(d => d.FarmId == r.FarmId && d.TenantId == _user.TenantId && d.DeletedAt == null)
            .OrderBy(d => d.Name)
            .Select(d => new DivisionDto(d.Id, d.FarmId, d.Name, d.MaxCapacity, d.IsActive, d.CreatedAt,
                _db.Animals.Count(a => a.DivisionId == d.Id && a.Status == AnimalStatus.Activo)))
            .ToListAsync(ct);
}
