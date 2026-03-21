using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Common.Dtos;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Admin.Queries;

public sealed record GetTenantsQuery(
    string? Search,
    int PageNumber = 1,
    int PageSize   = 20) : IRequest<PagedResult<TenantDto>>;

public sealed class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, PagedResult<TenantDto>>
{
    private readonly IApplicationDbContext _db;

    public GetTenantsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<TenantDto>> Handle(GetTenantsQuery req, CancellationToken ct)
    {
        var query = _db.Tenants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(s) ||
                t.PrimaryEmail.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(t => new TenantDto(
                t.Id, t.Name, t.PrimaryEmail,
                t.Status, t.Plan, t.PaidUntil, t.Notes, t.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<TenantDto>(items, total, req.PageNumber, req.PageSize);
    }
}
