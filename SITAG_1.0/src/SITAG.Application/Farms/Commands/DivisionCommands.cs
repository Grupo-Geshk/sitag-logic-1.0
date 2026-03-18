using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Farms.Dtos;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Farms.Commands;

public sealed record CreateDivisionCommand(Guid FarmId, string Name, int? MaxCapacity) : IRequest<DivisionDto>;

public sealed class CreateDivisionHandler : IRequestHandler<CreateDivisionCommand, DivisionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateDivisionHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<DivisionDto> Handle(CreateDivisionCommand r, CancellationToken ct)
    {
        var farmExists = await _db.Farms
            .AnyAsync(f => f.Id == r.FarmId && f.TenantId == _user.TenantId && f.DeletedAt == null, ct);
        if (!farmExists) throw new KeyNotFoundException($"Farm {r.FarmId} not found.");

        var div = new Division
        {
            TenantId    = _user.TenantId,
            FarmId      = r.FarmId,
            Name        = r.Name.Trim(),
            MaxCapacity = r.MaxCapacity,
        };
        _db.Divisions.Add(div);
        await _db.SaveChangesAsync(ct);
        return new DivisionDto(div.Id, div.FarmId, div.Name, div.MaxCapacity, div.IsActive, div.CreatedAt, 0);
    }
}

public sealed record UpdateDivisionCommand(Guid DivisionId, string Name, int? MaxCapacity) : IRequest<DivisionDto>;

public sealed class UpdateDivisionHandler : IRequestHandler<UpdateDivisionCommand, DivisionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UpdateDivisionHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<DivisionDto> Handle(UpdateDivisionCommand r, CancellationToken ct)
    {
        var div = await _db.Divisions
            .FirstOrDefaultAsync(d => d.Id == r.DivisionId && d.TenantId == _user.TenantId && d.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Division {r.DivisionId} not found.");

        div.Name        = r.Name.Trim();
        div.MaxCapacity = r.MaxCapacity;
        await _db.SaveChangesAsync(ct);
        var animalCount = await _db.Animals.CountAsync(a => a.DivisionId == div.Id && a.Status == AnimalStatus.Activo, ct);
        return new DivisionDto(div.Id, div.FarmId, div.Name, div.MaxCapacity, div.IsActive, div.CreatedAt, animalCount);
    }
}

public sealed record DeleteDivisionCommand(Guid DivisionId) : IRequest;

public sealed class DeleteDivisionHandler : IRequestHandler<DeleteDivisionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public DeleteDivisionHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(DeleteDivisionCommand r, CancellationToken ct)
    {
        var div = await _db.Divisions
            .FirstOrDefaultAsync(d => d.Id == r.DivisionId && d.TenantId == _user.TenantId && d.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Division {r.DivisionId} not found.");

        var hasAnimals = await _db.Animals
            .AnyAsync(a => a.DivisionId == r.DivisionId && a.Status == AnimalStatus.Activo, ct);
        if (hasAnimals)
            throw new InvalidOperationException("Cannot delete a division that has active animals.");

        div.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
