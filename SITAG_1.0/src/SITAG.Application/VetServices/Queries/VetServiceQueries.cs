using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.VetServices.Dtos;
using SITAG.Domain.Enums;

namespace SITAG.Application.VetServices.Queries;

public sealed record GetVetServicesQuery(
    Guid? FarmId, ServiceStatus? Status,
    int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<VetServiceDto>>;

public sealed class GetVetServicesHandler : IRequestHandler<GetVetServicesQuery, PagedResult<VetServiceDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetVetServicesHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<PagedResult<VetServiceDto>> Handle(GetVetServicesQuery r, CancellationToken ct)
    {
        var query = _db.VetServices.AsNoTracking()
            .Where(s => s.TenantId == _user.TenantId);
        if (r.FarmId.HasValue) query = query.Where(s => s.FarmId == r.FarmId);
        if (r.Status.HasValue) query = query.Where(s => s.Status == r.Status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.ScheduledDate)
            .Skip((r.PageNumber - 1) * r.PageSize)
            .Take(r.PageSize)
            .Select(s => new VetServiceDto(
                s.Id, s.TenantId, s.FarmId, s.DivisionId, s.WorkerId,
                s.ServiceType, s.Status, s.ScheduledDate, s.CompletedDate, s.Cost, s.Notes,
                s.ServiceAnimals.Select(sa => sa.AnimalId).ToList(),
                s.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<VetServiceDto>(items, total, r.PageNumber, r.PageSize);
    }
}

public sealed record GetServicesByAnimalQuery(Guid AnimalId) : IRequest<IReadOnlyList<VetServiceDto>>;

public sealed class GetServicesByAnimalHandler : IRequestHandler<GetServicesByAnimalQuery, IReadOnlyList<VetServiceDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetServicesByAnimalHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<VetServiceDto>> Handle(GetServicesByAnimalQuery r, CancellationToken ct) =>
        await _db.VetServices.AsNoTracking()
            .Where(s => s.TenantId == _user.TenantId && s.ServiceAnimals.Any(sa => sa.AnimalId == r.AnimalId))
            .OrderByDescending(s => s.ScheduledDate)
            .Select(s => new VetServiceDto(
                s.Id, s.TenantId, s.FarmId, s.DivisionId, s.WorkerId,
                s.ServiceType, s.Status, s.ScheduledDate, s.CompletedDate, s.Cost, s.Notes,
                s.ServiceAnimals.Select(sa => sa.AnimalId).ToList(),
                s.CreatedAt))
            .ToListAsync(ct);
}

public sealed record GetVetServiceByIdQuery(Guid ServiceId) : IRequest<VetServiceDto>;

public sealed class GetVetServiceByIdHandler : IRequestHandler<GetVetServiceByIdQuery, VetServiceDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetVetServiceByIdHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<VetServiceDto> Handle(GetVetServiceByIdQuery r, CancellationToken ct)
    {
        var s = await _db.VetServices.AsNoTracking()
            .Where(s => s.Id == r.ServiceId && s.TenantId == _user.TenantId)
            .Select(s => new VetServiceDto(
                s.Id, s.TenantId, s.FarmId, s.DivisionId, s.WorkerId,
                s.ServiceType, s.Status, s.ScheduledDate, s.CompletedDate, s.Cost, s.Notes,
                s.ServiceAnimals.Select(sa => sa.AnimalId).ToList(),
                s.CreatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Service {r.ServiceId} not found.");
        return s;
    }
}
