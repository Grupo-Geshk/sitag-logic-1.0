using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Producers.Dtos;

namespace SITAG.Application.Admin.Queries;

public sealed record GetProducersQuery : IRequest<IReadOnlyList<ProducerDto>>;

public sealed class GetProducersHandler : IRequestHandler<GetProducersQuery, IReadOnlyList<ProducerDto>>
{
    private readonly IApplicationDbContext _db;
    public GetProducersHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProducerDto>> Handle(GetProducersQuery _, CancellationToken ct) =>
        await _db.Producers
            .AsNoTracking()
            .OrderBy(p => p.DisplayName)
            .Select(p => new ProducerDto(p.Id, p.TenantId, p.DisplayName, p.CreatedAt))
            .ToListAsync(ct);
}
