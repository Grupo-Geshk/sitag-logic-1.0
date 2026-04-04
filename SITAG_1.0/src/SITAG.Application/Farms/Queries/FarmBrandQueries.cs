using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Farms.Dtos;

namespace SITAG.Application.Farms.Queries;

public sealed record GetFarmBrandsQuery(Guid FarmId) : IRequest<IReadOnlyList<FarmBrandDto>>;

public sealed class GetFarmBrandsHandler : IRequestHandler<GetFarmBrandsQuery, IReadOnlyList<FarmBrandDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetFarmBrandsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<FarmBrandDto>> Handle(GetFarmBrandsQuery r, CancellationToken ct) =>
        await _db.FarmBrands
            .AsNoTracking()
            .Where(b => b.FarmId == r.FarmId && b.TenantId == _user.TenantId)
            .OrderBy(b => b.Name)
            .Select(b => new FarmBrandDto(b.Id, b.FarmId, b.Name, b.PhotoUrl, b.CreatedAt))
            .ToListAsync(ct);
}
