using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Farms.Dtos;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Farms.Commands;

// ── Create ───────────────────────────────────────────────────────────────────
public sealed record CreateFarmCommand(
    string Name, string? Location, decimal? Hectares,
    string? FarmType, bool IsOwned) : IRequest<FarmDto>;

public sealed class CreateFarmHandler : IRequestHandler<CreateFarmCommand, FarmDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateFarmHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<FarmDto> Handle(CreateFarmCommand r, CancellationToken ct)
    {
        var farm = new Farm
        {
            TenantId = _user.TenantId,
            Name     = r.Name.Trim(),
            Location = r.Location?.Trim(),
            Hectares = r.Hectares,
            FarmType = r.FarmType?.Trim(),
            IsOwned  = r.IsOwned,
        };
        _db.Farms.Add(farm);
        await _db.SaveChangesAsync(ct);
        return Map(farm);
    }

    internal static FarmDto Map(Farm f) => new(
        f.Id, f.TenantId, f.Name, f.Location, f.Hectares, f.FarmType, f.IsOwned, f.CreatedAt);
}

// ── Update ───────────────────────────────────────────────────────────────────
public sealed record UpdateFarmCommand(
    Guid FarmId, string Name, string? Location,
    decimal? Hectares, string? FarmType, bool IsOwned) : IRequest<FarmDto>;

public sealed class UpdateFarmHandler : IRequestHandler<UpdateFarmCommand, FarmDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UpdateFarmHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<FarmDto> Handle(UpdateFarmCommand r, CancellationToken ct)
    {
        var farm = await _db.Farms
            .FirstOrDefaultAsync(f => f.Id == r.FarmId && f.TenantId == _user.TenantId && f.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Farm {r.FarmId} not found.");

        farm.Name     = r.Name.Trim();
        farm.Location = r.Location?.Trim();
        farm.Hectares = r.Hectares;
        farm.FarmType = r.FarmType?.Trim();
        farm.IsOwned  = r.IsOwned;

        await _db.SaveChangesAsync(ct);
        return CreateFarmHandler.Map(farm);
    }
}

// ── Soft Delete ───────────────────────────────────────────────────────────────
public sealed record DeleteFarmCommand(Guid FarmId) : IRequest;

public sealed class DeleteFarmHandler : IRequestHandler<DeleteFarmCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public DeleteFarmHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(DeleteFarmCommand r, CancellationToken ct)
    {
        var farm = await _db.Farms
            .FirstOrDefaultAsync(f => f.Id == r.FarmId && f.TenantId == _user.TenantId && f.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Farm {r.FarmId} not found.");

        farm.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
