using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Dashboard.Dtos;
using SITAG.Domain.Enums;

namespace SITAG.Application.Dashboard.Queries;

public sealed record GetDashboardAlertsQuery : IRequest<IReadOnlyList<DashboardAlertDto>>;

public sealed class GetDashboardAlertsHandler
    : IRequestHandler<GetDashboardAlertsQuery, IReadOnlyList<DashboardAlertDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public GetDashboardAlertsHandler(IApplicationDbContext db, ICurrentUser user)
    {
        _db   = db;
        _user = user;
    }

    public async Task<IReadOnlyList<DashboardAlertDto>> Handle(
        GetDashboardAlertsQuery _, CancellationToken ct)
    {
        var tid       = _user.TenantId;
        var alerts    = new List<DashboardAlertDto>();
        var expiryDay = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        // ── Sick / critical animals ───────────────────────────────────────────
        var sickAnimals = await _db.Animals
            .AsNoTracking()
            .Where(a => a.TenantId == tid
                     && a.Status == AnimalStatus.Activo
                     && (a.HealthStatus == AnimalHealthStatus.Enfermo
                      || a.HealthStatus == AnimalHealthStatus.Critico))
            .Select(a => new { a.Id, a.TagNumber, a.Name, a.HealthStatus })
            .ToListAsync(ct);

        foreach (var a in sickAnimals)
        {
            var severity = a.HealthStatus == AnimalHealthStatus.Critico ? "Alta" : "Media";
            var label    = string.IsNullOrEmpty(a.Name) ? a.TagNumber : $"{a.TagNumber} ({a.Name})";
            alerts.Add(new DashboardAlertDto(
                "ANIMAL_SICK", severity,
                $"Animal {label} está en estado {a.HealthStatus}.",
                a.Id, label));
        }

        // ── Low stock supplies ────────────────────────────────────────────────
        var lowStock = await _db.Supplies
            .AsNoTracking()
            .Where(s => s.TenantId == tid
                     && s.DeletedAt == null
                     && s.CurrentQuantity <= s.MinStockLevel)
            .Select(s => new { s.Id, s.Name, s.CurrentQuantity, s.Unit, s.MinStockLevel })
            .ToListAsync(ct);

        foreach (var s in lowStock)
            alerts.Add(new DashboardAlertDto(
                "LOW_STOCK", "Media",
                $"Insumo '{s.Name}' con stock bajo: {s.CurrentQuantity} {s.Unit} (mínimo {s.MinStockLevel}).",
                s.Id, s.Name));

        // ── Expiring supplies (≤ 30 days) ────────────────────────────────────
        var expiring = await _db.Supplies
            .AsNoTracking()
            .Where(s => s.TenantId == tid
                     && s.DeletedAt == null
                     && s.ExpirationDate.HasValue
                     && s.ExpirationDate.Value <= expiryDay)
            .Select(s => new { s.Id, s.Name, s.ExpirationDate })
            .ToListAsync(ct);

        foreach (var s in expiring)
        {
            var days     = (s.ExpirationDate!.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days;
            var severity = days <= 7 ? "Alta" : "Baja";
            alerts.Add(new DashboardAlertDto(
                "EXPIRING_SUPPLY", severity,
                days <= 0
                    ? $"Insumo '{s.Name}' ha vencido."
                    : $"Insumo '{s.Name}' vence en {days} día(s).",
                s.Id, s.Name));
        }

        // ── Overdue services (Pendiente + ScheduledDate in the past) ─────────
        var now = DateTimeOffset.UtcNow;
        var overdueServices = await _db.VetServices
            .AsNoTracking()
            .Where(s => s.TenantId == tid
                     && s.Status == ServiceStatus.Pendiente
                     && s.ScheduledDate < now)
            .Select(s => new { s.Id, s.ServiceType, s.ScheduledDate })
            .ToListAsync(ct);

        foreach (var svc in overdueServices)
        {
            var days = (int)(now - svc.ScheduledDate).TotalDays;
            alerts.Add(new DashboardAlertDto(
                "OVERDUE_SERVICE", "Alta",
                $"Servicio '{svc.ServiceType}' programado hace {days} día(s) no ha sido completado.",
                svc.Id, svc.ServiceType));
        }

        return alerts.OrderByDescending(a => a.Severity == "Alta" ? 2 : a.Severity == "Media" ? 1 : 0).ToList();
    }
}
