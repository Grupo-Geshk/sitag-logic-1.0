using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Enums;

namespace SITAG.Application.Admin.Queries;

public sealed record GetAdminStatisticsQuery : IRequest<AdminStatisticsDto>;

public sealed class GetAdminStatisticsQueryHandler
    : IRequestHandler<GetAdminStatisticsQuery, AdminStatisticsDto>
{
    private readonly IApplicationDbContext _db;

    public GetAdminStatisticsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<AdminStatisticsDto> Handle(GetAdminStatisticsQuery req, CancellationToken ct)
    {
        var tenantCounts = await _db.Tenants
            .AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Get(TenantStatus s) => tenantCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        var totalUsers     = await _db.Users.AsNoTracking().CountAsync(ct);
        var totalProducers = await _db.Producers.AsNoTracking().CountAsync(ct);

        // Animals and Farms counts — use 0 if tables exist but are empty
        var totalAnimals = await _db.Animals.AsNoTracking().CountAsync(ct);
        var totalFarms   = await _db.Farms.AsNoTracking().CountAsync(ct);

        return new AdminStatisticsDto(
            TotalTenants      : tenantCounts.Sum(x => x.Count),
            ActiveTenants     : Get(TenantStatus.Active),
            PastDueTenants    : Get(TenantStatus.PastDue),
            DelinquentTenants : Get(TenantStatus.Delinquent),
            TotalUsers        : totalUsers,
            TotalProducers    : totalProducers,
            TotalAnimals      : totalAnimals,
            TotalFarms        : totalFarms);
    }
}
