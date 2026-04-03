using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Supplies.Commands;
using SITAG.Application.Supplies.Dtos;

namespace SITAG.Application.Supplies.Queries;

public sealed record GetSupplyLotsQuery(Guid SupplyId) : IRequest<IReadOnlyList<SupplyLotDto>>;

public sealed class GetSupplyLotsHandler : IRequestHandler<GetSupplyLotsQuery, IReadOnlyList<SupplyLotDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetSupplyLotsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<SupplyLotDto>> Handle(GetSupplyLotsQuery r, CancellationToken ct)
    {
        // Verify supply belongs to tenant
        var exists = await _db.Supplies.AnyAsync(
            s => s.Id == r.SupplyId && s.TenantId == _user.TenantId && s.DeletedAt == null, ct);
        if (!exists) throw new KeyNotFoundException($"Supply {r.SupplyId} not found.");

        return await _db.SupplyLots
            .Where(l => l.SupplyId == r.SupplyId && l.TenantId == _user.TenantId)
            .OrderByDescending(l => l.PurchaseDate)
            .ThenByDescending(l => l.CreatedAt)
            .Select(l => SupplyLotMapper.ToDto(l))
            .ToListAsync(ct);
    }
}
