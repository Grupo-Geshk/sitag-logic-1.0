using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Dashboard.Dtos;
using SITAG.Domain.Enums;

namespace SITAG.Application.Dashboard.Queries;

public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed class GetDashboardHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public GetDashboardHandler(IApplicationDbContext db, ICurrentUser user)
    {
        _db   = db;
        _user = user;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery _, CancellationToken ct)
    {
        var tid      = _user.TenantId;
        var now      = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
        var birth30  = DateOnly.FromDateTime(now.AddDays(-30).DateTime);
        var close30  = now.AddDays(-30);

        // ── Animal KPIs ──────────────────────────────────────────────────────
        var animals = await _db.Animals
            .AsNoTracking()
            .Where(a => a.TenantId == tid)
            .Select(a => new
            {
                a.Status,
                a.HealthStatus,
                a.BirthDate,
                a.ClosedAt,
            })
            .ToListAsync(ct);

        int totalAnimales    = animals.Count;
        int animalesActivos  = animals.Count(a => a.Status == AnimalStatus.Activo);
        int animalesEnfermos = animals.Count(a =>
            a.Status == AnimalStatus.Activo &&
            (a.HealthStatus == AnimalHealthStatus.Enfermo || a.HealthStatus == AnimalHealthStatus.Critico));
        int natalidad30 = animals.Count(a =>
            a.BirthDate.HasValue && a.BirthDate.Value >= birth30);
        int mortalidad30 = animals.Count(a =>
            a.Status == AnimalStatus.Muerto && a.ClosedAt.HasValue && a.ClosedAt.Value >= close30);

        // ── Economy KPIs (current month) ─────────────────────────────────────
        var txns = await _db.EconomyTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == tid && t.DeletedAt == null && t.TxnDate >= monthStart)
            .Select(t => new { t.Type, t.Amount })
            .ToListAsync(ct);

        decimal ingresosMes = txns.Where(t => t.Type == TransactionType.Ingreso).Sum(t => t.Amount);
        decimal egresosMes  = txns.Where(t => t.Type == TransactionType.Egreso).Sum(t => t.Amount);

        // ── Distribution by farm ──────────────────────────────────────────────
        var distribution = await _db.Farms
            .AsNoTracking()
            .Where(f => f.TenantId == tid && f.DeletedAt == null)
            .Select(f => new
            {
                f.Id,
                f.Name,
                ActiveAnimals = f.Animals.Count(a => a.Status == AnimalStatus.Activo)
            })
            .OrderByDescending(f => f.ActiveAnimals)
            .Select(f => new FarmDistributionDto(f.Id, f.Name, f.ActiveAnimals))
            .ToListAsync(ct);

        return new DashboardDto(
            totalAnimales, animalesActivos, animalesEnfermos,
            natalidad30, mortalidad30,
            ingresosMes, egresosMes, ingresosMes - egresosMes,
            distribution);
    }
}
