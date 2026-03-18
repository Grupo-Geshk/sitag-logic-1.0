using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Admin.Queries;

public sealed record GetTenantByIdQuery(Guid TenantId) : IRequest<TenantDetailDto>;

public sealed class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetTenantByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<TenantDetailDto> Handle(GetTenantByIdQuery req, CancellationToken ct)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == req.TenantId)
            .Select(t => new
            {
                t.Id, t.Name, t.PrimaryEmail, t.Status,
                t.PaidUntil, t.Notes, t.CreatedAt,
                UserCount = t.Users.Count
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Tenant {req.TenantId} not found.");

        return new TenantDetailDto(
            tenant.Id, tenant.Name, tenant.PrimaryEmail,
            tenant.Status, tenant.PaidUntil, tenant.Notes,
            tenant.CreatedAt, tenant.UserCount);
    }
}
