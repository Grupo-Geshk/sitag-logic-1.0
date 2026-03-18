using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Producers.Dtos;

namespace SITAG.Application.Producers.Commands;

public sealed record UpdateProducerCommand(string DisplayName) : IRequest<ProducerDto>;

public sealed class UpdateProducerHandler : IRequestHandler<UpdateProducerCommand, ProducerDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UpdateProducerHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<ProducerDto> Handle(UpdateProducerCommand r, CancellationToken ct)
    {
        var p = await _db.Producers
            .FirstOrDefaultAsync(p => p.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException("Producer profile not found for this tenant.");

        p.DisplayName = r.DisplayName.Trim();
        await _db.SaveChangesAsync(ct);
        return new ProducerDto(p.Id, p.TenantId, p.DisplayName, p.CreatedAt);
    }
}
