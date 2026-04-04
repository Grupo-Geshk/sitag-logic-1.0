using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Farms.Dtos;
using SITAG.Domain.Entities;

namespace SITAG.Application.Farms.Commands;

// ── Create ────────────────────────────────────────────────────────────────────
public sealed record CreateFarmBrandCommand(
    Guid FarmId, string Name, string? PhotoUrl) : IRequest<FarmBrandDto>;

public sealed class CreateFarmBrandHandler : IRequestHandler<CreateFarmBrandCommand, FarmBrandDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateFarmBrandHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<FarmBrandDto> Handle(CreateFarmBrandCommand r, CancellationToken ct)
    {
        var farmExists = await _db.Farms
            .AnyAsync(f => f.Id == r.FarmId && f.TenantId == _user.TenantId && f.DeletedAt == null, ct);
        if (!farmExists) throw new KeyNotFoundException($"Farm {r.FarmId} not found.");

        var brand = new FarmBrand
        {
            TenantId = _user.TenantId,
            FarmId   = r.FarmId,
            Name     = r.Name.Trim(),
            PhotoUrl = r.PhotoUrl?.Trim(),
        };
        _db.FarmBrands.Add(brand);
        await _db.SaveChangesAsync(ct);

        return new FarmBrandDto(brand.Id, brand.FarmId, brand.Name, brand.PhotoUrl, brand.CreatedAt);
    }
}

// ── Update ────────────────────────────────────────────────────────────────────
public sealed record UpdateFarmBrandCommand(
    Guid BrandId, string Name, string? PhotoUrl) : IRequest<FarmBrandDto>;

public sealed class UpdateFarmBrandHandler : IRequestHandler<UpdateFarmBrandCommand, FarmBrandDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UpdateFarmBrandHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<FarmBrandDto> Handle(UpdateFarmBrandCommand r, CancellationToken ct)
    {
        var brand = await _db.FarmBrands
            .FirstOrDefaultAsync(b => b.Id == r.BrandId && b.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Brand {r.BrandId} not found.");

        brand.Name     = r.Name.Trim();
        brand.PhotoUrl = r.PhotoUrl?.Trim();
        await _db.SaveChangesAsync(ct);

        return new FarmBrandDto(brand.Id, brand.FarmId, brand.Name, brand.PhotoUrl, brand.CreatedAt);
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────
public sealed record DeleteFarmBrandCommand(Guid BrandId) : IRequest;

public sealed class DeleteFarmBrandHandler : IRequestHandler<DeleteFarmBrandCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public DeleteFarmBrandHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(DeleteFarmBrandCommand r, CancellationToken ct)
    {
        var brand = await _db.FarmBrands
            .FirstOrDefaultAsync(b => b.Id == r.BrandId && b.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Brand {r.BrandId} not found.");

        _db.FarmBrands.Remove(brand);
        await _db.SaveChangesAsync(ct);
    }
}

// ── Assign brand to animal ────────────────────────────────────────────────────
public sealed record AssignAnimalBrandCommand(
    Guid AnimalId, Guid? BrandId, DateTimeOffset? BrandedAt) : IRequest;

public sealed class AssignAnimalBrandHandler : IRequestHandler<AssignAnimalBrandCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public AssignAnimalBrandHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(AssignAnimalBrandCommand r, CancellationToken ct)
    {
        var animal = await _db.Animals
            .FirstOrDefaultAsync(a => a.Id == r.AnimalId && a.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Animal {r.AnimalId} not found.");

        if (r.BrandId.HasValue)
        {
            var brandExists = await _db.FarmBrands
                .AnyAsync(b => b.Id == r.BrandId.Value && b.TenantId == _user.TenantId, ct);
            if (!brandExists) throw new KeyNotFoundException($"Brand {r.BrandId} not found.");
        }

        animal.BrandId   = r.BrandId;
        animal.BrandedAt = r.BrandId.HasValue ? (r.BrandedAt ?? DateTimeOffset.UtcNow) : null;
        await _db.SaveChangesAsync(ct);
    }
}
