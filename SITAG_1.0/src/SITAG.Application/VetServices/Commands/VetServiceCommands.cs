using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.VetServices.Dtos;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.VetServices.Commands;

internal static class VetServiceMapper
{
    internal static VetServiceDto ToDto(VetService s, IReadOnlyList<Guid> animalIds) => new(
        s.Id, s.TenantId, s.FarmId, s.DivisionId, s.WorkerId,
        s.ServiceType, s.Status, s.ScheduledDate, s.CompletedDate,
        s.Cost, s.Notes, animalIds, s.CreatedAt);
}

// ── Create ────────────────────────────────────────────────────────────────────
public sealed record CreateVetServiceCommand(
    string ServiceType, DateTimeOffset ScheduledDate,
    Guid FarmId, Guid? DivisionId, Guid? WorkerId,
    decimal? Cost, string? Notes, List<Guid> AnimalIds) : IRequest<VetServiceDto>;

public sealed class CreateVetServiceHandler : IRequestHandler<CreateVetServiceCommand, VetServiceDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateVetServiceHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<VetServiceDto> Handle(CreateVetServiceCommand r, CancellationToken ct)
    {
        var svc = new VetService
        {
            TenantId      = _user.TenantId,
            ServiceType   = r.ServiceType.Trim(),
            ScheduledDate = r.ScheduledDate,
            FarmId        = r.FarmId,
            DivisionId    = r.DivisionId,
            WorkerId      = r.WorkerId,
            Cost          = r.Cost,
            Notes         = r.Notes?.Trim(),
        };
        _db.VetServices.Add(svc);

        foreach (var aid in r.AnimalIds.Distinct())
            _db.ServiceAnimals.Add(new ServiceAnimal { TenantId = _user.TenantId, ServiceId = svc.Id, AnimalId = aid });

        await _db.SaveChangesAsync(ct);
        return VetServiceMapper.ToDto(svc, r.AnimalIds);
    }
}

// ── Update ────────────────────────────────────────────────────────────────────
public sealed record UpdateVetServiceCommand(
    Guid ServiceId,
    string ServiceType, DateTimeOffset ScheduledDate,
    Guid FarmId, Guid? DivisionId, Guid? WorkerId,
    decimal? Cost, string? Notes, List<Guid> AnimalIds) : IRequest<VetServiceDto>;

public sealed class UpdateVetServiceHandler : IRequestHandler<UpdateVetServiceCommand, VetServiceDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UpdateVetServiceHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<VetServiceDto> Handle(UpdateVetServiceCommand r, CancellationToken ct)
    {
        var svc = await _db.VetServices
            .FirstOrDefaultAsync(s => s.Id == r.ServiceId && s.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Service {r.ServiceId} not found.");

        if (svc.Status == ServiceStatus.Completado)
            throw new InvalidOperationException("Cannot update a completed service.");

        svc.ServiceType   = r.ServiceType.Trim();
        svc.ScheduledDate = r.ScheduledDate;
        svc.FarmId        = r.FarmId;
        svc.DivisionId    = r.DivisionId;
        svc.WorkerId      = r.WorkerId;
        svc.Cost          = r.Cost;
        svc.Notes         = r.Notes?.Trim();

        // Replace animal assignments
        var existing = await _db.ServiceAnimals
            .Where(sa => sa.ServiceId == svc.Id && sa.TenantId == _user.TenantId)
            .ToListAsync(ct);
        _db.ServiceAnimals.RemoveRange(existing);

        var distinctIds = r.AnimalIds.Distinct().ToList();
        foreach (var aid in distinctIds)
            _db.ServiceAnimals.Add(new ServiceAnimal { TenantId = _user.TenantId, ServiceId = svc.Id, AnimalId = aid });

        await _db.SaveChangesAsync(ct);
        return VetServiceMapper.ToDto(svc, distinctIds);
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────
public sealed record DeleteVetServiceCommand(Guid ServiceId) : IRequest;

public sealed class DeleteVetServiceHandler : IRequestHandler<DeleteVetServiceCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public DeleteVetServiceHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(DeleteVetServiceCommand r, CancellationToken ct)
    {
        var svc = await _db.VetServices
            .FirstOrDefaultAsync(s => s.Id == r.ServiceId && s.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Service {r.ServiceId} not found.");

        if (svc.Status == ServiceStatus.Completado)
            throw new InvalidOperationException("Cannot delete a completed service.");

        _db.VetServices.Remove(svc);
        await _db.SaveChangesAsync(ct);
    }
}

// ── Complete ──────────────────────────────────────────────────────────────────
public sealed record CompleteVetServiceCommand(
    Guid ServiceId, DateTimeOffset? CompletedDate, decimal? Cost, string? Notes) : IRequest<VetServiceDto>;

public sealed class CompleteVetServiceHandler : IRequestHandler<CompleteVetServiceCommand, VetServiceDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CompleteVetServiceHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<VetServiceDto> Handle(CompleteVetServiceCommand r, CancellationToken ct)
    {
        var svc = await _db.VetServices
            .FirstOrDefaultAsync(s => s.Id == r.ServiceId && s.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Service {r.ServiceId} not found.");

        svc.Status        = ServiceStatus.Completado;
        svc.CompletedDate = r.CompletedDate ?? DateTimeOffset.UtcNow;
        if (r.Cost.HasValue) svc.Cost = r.Cost;
        if (r.Notes is not null) svc.Notes = r.Notes.Trim();

        await _db.SaveChangesAsync(ct);

        var animalIds = await _db.ServiceAnimals
            .Where(sa => sa.ServiceId == svc.Id)
            .Select(sa => sa.AnimalId)
            .ToListAsync(ct);

        return VetServiceMapper.ToDto(svc, animalIds);
    }
}
