using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Admin.Queries;

public record TenantSummaryDto(
    int Users,
    int Animals,
    int Farms,
    int Divisions,
    int Workers,
    int VetServices,
    int Supplies,
    int Transactions,
    int AnimalEvents);

public sealed record GetTenantSummaryQuery(Guid TenantId) : IRequest<TenantSummaryDto>;

public sealed class GetTenantSummaryHandler : IRequestHandler<GetTenantSummaryQuery, TenantSummaryDto>
{
    private readonly IApplicationDbContext _db;
    public GetTenantSummaryHandler(IApplicationDbContext db) => _db = db;

    public async Task<TenantSummaryDto> Handle(GetTenantSummaryQuery req, CancellationToken ct)
    {
        var tid = req.TenantId;

        var users        = await _db.Users.CountAsync(x => x.TenantId == tid && x.DeletedAt == null, ct);
        var animals      = await _db.Animals.CountAsync(x => x.TenantId == tid, ct);
        var farms        = await _db.Farms.CountAsync(x => x.TenantId == tid, ct);
        var divisions    = await _db.Divisions.CountAsync(x => x.TenantId == tid, ct);
        var workers      = await _db.Workers.CountAsync(x => x.TenantId == tid, ct);
        var services     = await _db.VetServices.CountAsync(x => x.TenantId == tid, ct);
        var supplies     = await _db.Supplies.CountAsync(x => x.TenantId == tid, ct);
        var transactions = await _db.EconomyTransactions.CountAsync(x => x.TenantId == tid, ct);
        var events       = await _db.AnimalEvents.CountAsync(x => x.TenantId == tid, ct);

        return new TenantSummaryDto(users, animals, farms, divisions, workers,
                                    services, supplies, transactions, events);
    }
}
