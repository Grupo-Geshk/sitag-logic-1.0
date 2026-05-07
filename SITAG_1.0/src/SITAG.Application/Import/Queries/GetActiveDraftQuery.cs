using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Import.Commands;
using SITAG.Application.Import.Dtos;
using SITAG.Domain.Enums;

namespace SITAG.Application.Import.Queries;

public sealed record GetActiveDraftQuery : IRequest<ImportDraftDto?>;

public sealed class GetActiveDraftHandler : IRequestHandler<GetActiveDraftQuery, ImportDraftDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public GetActiveDraftHandler(IApplicationDbContext db, ICurrentUser user)
    {
        _db = db; _user = user;
    }

    public async Task<ImportDraftDto?> Handle(GetActiveDraftQuery r, CancellationToken ct)
    {
        var draft = await _db.ImportDrafts
            .Where(d => d.TenantId == _user.TenantId && d.Status == ImportDraftStatus.Pending)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return draft is null ? null : ImportDraftMapper.ToDto(draft);
    }
}
