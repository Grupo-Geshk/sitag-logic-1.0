using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Producers.Dtos;

namespace SITAG.Application.Producers.Queries;

public sealed record GetProducerQuery : IRequest<ProducerDto>;

public sealed class GetProducerHandler : IRequestHandler<GetProducerQuery, ProducerDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetProducerHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<ProducerDto> Handle(GetProducerQuery _, CancellationToken ct)
    {
        var p = await _db.Producers
            .AsNoTracking()
            .Where(p => p.TenantId == _user.TenantId)
            .Select(p => new ProducerDto(p.Id, p.TenantId, p.DisplayName, p.CreatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Producer profile not found for this tenant.");
        return p;
    }
}
