using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Farms.Dtos;

namespace SITAG.Application.Farms.Queries;

public sealed record GetAllBrandsQuery : IRequest<IReadOnlyList<FarmBrandDto>>;

public sealed class GetAllBrandsHandler : IRequestHandler<GetAllBrandsQuery, IReadOnlyList<FarmBrandDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetAllBrandsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<FarmBrandDto>> Handle(GetAllBrandsQuery _, CancellationToken ct) =>
        await _db.FarmBrands
            .AsNoTracking()
            .Where(b => b.TenantId == _user.TenantId)
            .OrderBy(b => b.Name)
            .Select(b => new FarmBrandDto(b.Id, b.Name, b.PhotoUrl, b.CreatedAt))
            .ToListAsync(ct);
}
