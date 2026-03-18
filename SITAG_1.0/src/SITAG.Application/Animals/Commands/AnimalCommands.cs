using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Animals.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Animals.Commands;

// ── Shared mapper ─────────────────────────────────────────────────────────────
internal static class AnimalMapper
{
    internal static AnimalDto ToDto(Animal a) => new(
        a.Id, a.TenantId, a.TagNumber, a.Name, a.Breed, a.Sex,
        a.BirthDate, a.Weight, a.Status, a.HealthStatus,
        a.FarmId, a.DivisionId,
        a.MotherId, a.MotherRef,
        a.FatherId, a.FatherRef,
        a.PhotoUrl,
        ParentId: a.MotherId,      // backward-compat alias
        a.CloseReason, a.ClosedAt, a.CreatedAt);
}

// ── Create ────────────────────────────────────────────────────────────────────
// ParentId is accepted as a legacy alias for MotherId (frontend backward compat).
// Both cannot be meaningfully different — if both are provided, MotherId wins.
public sealed record CreateAnimalCommand(
    string TagNumber, string? Name, string? Breed, string Sex,
    DateOnly? BirthDate, decimal? Weight,
    Guid FarmId, Guid? DivisionId,
    // Genealogy
    Guid? MotherId = null, string? MotherRef = null,
    Guid? FatherId = null, string? FatherRef = null,
    // Photo
    string? PhotoUrl = null,
    // Legacy alias — used when the old frontend sends parentId instead of motherId
    Guid? ParentId = null) : IRequest<AnimalDto>;

public sealed class CreateAnimalHandler : IRequestHandler<CreateAnimalCommand, AnimalDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateAnimalHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<AnimalDto> Handle(CreateAnimalCommand r, CancellationToken ct)
    {
        var tid = _user.TenantId;

        // Resolve effective motherId (new field wins over legacy ParentId alias)
        var effectiveMotherId = r.MotherId ?? r.ParentId;

        if (await _db.Animals.AnyAsync(a => a.TagNumber == r.TagNumber && a.TenantId == tid, ct))
            throw new InvalidOperationException($"El número de arete '{r.TagNumber}' ya existe.");

        // ── Domain validations ─────────────────────────────────────────────────
        if (effectiveMotherId.HasValue)
        {
            var mother = await _db.Animals
                .Where(a => a.Id == effectiveMotherId && a.TenantId == tid)
                .Select(a => new { a.Id, a.Sex })
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException($"La madre especificada no existe o no pertenece a este productor.");

            if (mother.Sex != "Hembra")
                throw new InvalidOperationException("El animal seleccionado como madre debe ser Hembra.");
        }

        if (r.FatherId.HasValue)
        {
            if (r.FatherId == effectiveMotherId)
                throw new InvalidOperationException("La madre y el padre no pueden ser el mismo animal.");

            var father = await _db.Animals
                .Where(a => a.Id == r.FatherId && a.TenantId == tid)
                .Select(a => new { a.Id, a.Sex })
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException($"El padre especificado no existe o no pertenece a este productor.");

            if (father.Sex != "Macho")
                throw new InvalidOperationException("El animal seleccionado como padre debe ser Macho.");
        }

        var animal = new Animal
        {
            TenantId   = tid,
            TagNumber  = r.TagNumber.Trim(),
            Name       = r.Name?.Trim(),
            Breed      = r.Breed?.Trim(),
            Sex        = r.Sex,
            BirthDate  = r.BirthDate,
            Weight     = r.Weight,
            FarmId     = r.FarmId,
            DivisionId = r.DivisionId,
            MotherId   = effectiveMotherId,
            MotherRef  = r.MotherRef?.Trim(),
            FatherId   = r.FatherId,
            FatherRef  = r.FatherRef?.Trim(),
            PhotoUrl   = r.PhotoUrl?.Trim(),
        };
        _db.Animals.Add(animal);
        await _db.SaveChangesAsync(ct);
        return AnimalMapper.ToDto(animal);
    }
}

// ── Update ────────────────────────────────────────────────────────────────────
// Genealogy FKs (MotherId / FatherId) are immutable post-creation.
// MotherRef / FatherRef (free-text external refs) are mutable for corrections.
// PhotoUrl is mutable — updated whenever the producer uploads a new photo.
public sealed record UpdateAnimalCommand(
    Guid AnimalId, string? Name, string? Breed, string Sex,
    DateOnly? BirthDate, decimal? Weight,
    Guid FarmId, Guid? DivisionId,
    string? PhotoUrl = null,
    string? MotherRef = null,
    string? FatherRef = null) : IRequest<AnimalDto>;

public sealed class UpdateAnimalHandler : IRequestHandler<UpdateAnimalCommand, AnimalDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UpdateAnimalHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<AnimalDto> Handle(UpdateAnimalCommand r, CancellationToken ct)
    {
        var a = await _db.Animals
            .FirstOrDefaultAsync(a => a.Id == r.AnimalId && a.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Animal {r.AnimalId} no encontrado.");

        a.Name       = r.Name?.Trim();
        a.Breed      = r.Breed?.Trim();
        a.Sex        = r.Sex;
        a.BirthDate  = r.BirthDate;
        a.Weight     = r.Weight;
        a.FarmId     = r.FarmId;
        a.DivisionId = r.DivisionId;
        a.PhotoUrl   = r.PhotoUrl?.Trim();
        a.MotherRef  = r.MotherRef?.Trim();
        a.FatherRef  = r.FatherRef?.Trim();
        await _db.SaveChangesAsync(ct);
        return AnimalMapper.ToDto(a);
    }
}

// ── Update Health ─────────────────────────────────────────────────────────────
public sealed record UpdateAnimalHealthCommand(
    Guid AnimalId, AnimalHealthStatus HealthStatus, string? Notes) : IRequest<AnimalDto>;

public sealed class UpdateAnimalHealthHandler : IRequestHandler<UpdateAnimalHealthCommand, AnimalDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UpdateAnimalHealthHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<AnimalDto> Handle(UpdateAnimalHealthCommand r, CancellationToken ct)
    {
        var a = await _db.Animals
            .FirstOrDefaultAsync(a => a.Id == r.AnimalId && a.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Animal {r.AnimalId} no encontrado.");

        a.HealthStatus = r.HealthStatus;

        if (!string.IsNullOrWhiteSpace(r.Notes))
        {
            var ev = new AnimalEvent
            {
                TenantId        = _user.TenantId,
                AnimalId        = a.Id,
                EventType       = AnimalEventType.Tratamiento,
                EventDate       = DateTimeOffset.UtcNow,
                FarmId          = a.FarmId,
                Description     = r.Notes,
                CreatedByUserId = _user.UserId,
            };
            _db.AnimalEvents.Add(ev);
        }

        await _db.SaveChangesAsync(ct);
        return AnimalMapper.ToDto(a);
    }
}

// ── Close animal (sold / dead) ────────────────────────────────────────────────
public sealed record CloseAnimalCommand(
    Guid AnimalId, AnimalStatus Outcome, string? Reason) : IRequest<AnimalDto>;

public sealed class CloseAnimalHandler : IRequestHandler<CloseAnimalCommand, AnimalDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CloseAnimalHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<AnimalDto> Handle(CloseAnimalCommand r, CancellationToken ct)
    {
        if (r.Outcome == AnimalStatus.Activo)
            throw new ArgumentException("Outcome must be Vendido or Muerto.");

        var a = await _db.Animals
            .FirstOrDefaultAsync(a => a.Id == r.AnimalId && a.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Animal {r.AnimalId} no encontrado.");

        a.Status      = r.Outcome;
        a.CloseReason = r.Reason?.Trim();
        a.ClosedAt    = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return AnimalMapper.ToDto(a);
    }
}

// ── Purchase (Animal + EconomyTransaction, atomic) ────────────────────────────
public sealed record PurchaseAnimalCommand(
    string TagNumber, string? Name, string? Breed, string Sex,
    DateOnly? BirthDate, decimal? Weight,
    Guid FarmId, Guid? DivisionId,
    decimal PurchasePrice, DateTimeOffset PurchaseDate,
    Guid? CategoryId, string? CategoryName,
    string? PhotoUrl = null) : IRequest<AnimalDto>;

public sealed class PurchaseAnimalHandler : IRequestHandler<PurchaseAnimalCommand, AnimalDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public PurchaseAnimalHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<AnimalDto> Handle(PurchaseAnimalCommand r, CancellationToken ct)
    {
        var tid = _user.TenantId;
        if (await _db.Animals.AnyAsync(a => a.TagNumber == r.TagNumber && a.TenantId == tid, ct))
            throw new InvalidOperationException($"El número de arete '{r.TagNumber}' ya existe.");

        var animal = new Animal
        {
            TenantId   = tid, TagNumber  = r.TagNumber.Trim(), Name = r.Name?.Trim(),
            Breed      = r.Breed?.Trim(), Sex = r.Sex, BirthDate = r.BirthDate,
            Weight     = r.Weight, FarmId = r.FarmId, DivisionId = r.DivisionId,
            PhotoUrl   = r.PhotoUrl?.Trim(),
        };

        var txn = new EconomyTransaction
        {
            TenantId         = tid,
            Type             = TransactionType.Egreso,
            CategoryId       = r.CategoryId,
            CategoryName     = r.CategoryName ?? "Compra de Ganado",
            Description      = $"Compra de animal {r.TagNumber}",
            Amount           = r.PurchasePrice,
            TxnDate          = r.PurchaseDate,
            FarmId           = r.FarmId,
            CreatedByUserId  = _user.UserId,
        };

        _db.Animals.Add(animal);
        _db.EconomyTransactions.Add(txn);
        await _db.SaveChangesAsync(ct);
        return AnimalMapper.ToDto(animal);
    }
}

// ── Bulk movement ─────────────────────────────────────────────────────────────
public sealed record BulkMoveAnimalsCommand(
    List<Guid> AnimalIds, Guid ToFarmId, Guid? ToDivisionId, string? Reason) : IRequest<int>;

public sealed class BulkMoveAnimalsHandler : IRequestHandler<BulkMoveAnimalsCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public BulkMoveAnimalsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<int> Handle(BulkMoveAnimalsCommand r, CancellationToken ct)
    {
        var tid = _user.TenantId;
        var animals = await _db.Animals
            .Where(a => r.AnimalIds.Contains(a.Id) && a.TenantId == tid && a.Status == AnimalStatus.Activo)
            .ToListAsync(ct);

        foreach (var a in animals)
        {
            var scope = a.FarmId == r.ToFarmId ? MovementScope.Interna : MovementScope.EntreFincas;
            var mv = new AnimalMovement
            {
                TenantId        = tid,
                AnimalId        = a.Id,
                FromFarmId      = a.FarmId,
                FromDivisionId  = a.DivisionId,
                ToFarmId        = r.ToFarmId,
                ToDivisionId    = r.ToDivisionId,
                MovementDate    = DateTimeOffset.UtcNow,
                Scope           = scope,
                Reason          = r.Reason?.Trim(),
                CreatedByUserId = _user.UserId,
            };
            a.FarmId     = r.ToFarmId;
            a.DivisionId = r.ToDivisionId;
            _db.AnimalMovements.Add(mv);
        }

        await _db.SaveChangesAsync(ct);
        return animals.Count;
    }
}

// ── Single animal movement ────────────────────────────────────────────────────
public sealed record MoveAnimalCommand(
    Guid AnimalId, Guid ToFarmId, Guid? ToDivisionId,
    DateTimeOffset? MovementDate, string? Reason) : IRequest<AnimalMovementDto>;

public sealed class MoveAnimalHandler : IRequestHandler<MoveAnimalCommand, AnimalMovementDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public MoveAnimalHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<AnimalMovementDto> Handle(MoveAnimalCommand r, CancellationToken ct)
    {
        var tid = _user.TenantId;
        var a = await _db.Animals
            .FirstOrDefaultAsync(a => a.Id == r.AnimalId && a.TenantId == tid && a.Status == AnimalStatus.Activo, ct)
            ?? throw new KeyNotFoundException($"Animal {r.AnimalId} no encontrado o no está activo.");

        var scope = a.FarmId == r.ToFarmId ? MovementScope.Interna : MovementScope.EntreFincas;
        var mv = new AnimalMovement
        {
            TenantId        = tid,
            AnimalId        = a.Id,
            FromFarmId      = a.FarmId,
            FromDivisionId  = a.DivisionId,
            ToFarmId        = r.ToFarmId,
            ToDivisionId    = r.ToDivisionId,
            MovementDate    = r.MovementDate ?? DateTimeOffset.UtcNow,
            Scope           = scope,
            Reason          = r.Reason?.Trim(),
            CreatedByUserId = _user.UserId,
        };
        a.FarmId     = r.ToFarmId;
        a.DivisionId = r.ToDivisionId;
        _db.AnimalMovements.Add(mv);
        await _db.SaveChangesAsync(ct);

        return new AnimalMovementDto(mv.Id, mv.AnimalId, mv.FromFarmId, mv.FromDivisionId,
            mv.ToFarmId, mv.ToDivisionId, mv.MovementDate, mv.Scope, mv.Reason, mv.CreatedAt);
    }
}

// ── Offspring data for birth events ──────────────────────────────────────────
public sealed record OffspringData(
    string TagNumber, string Sex,
    string? Name, string? Breed, decimal? Weight);

// ── Record animal event (+ atomic EconomyTransaction for Compra/Venta) ───────
public sealed record CreateAnimalEventCommand(
    Guid AnimalId, AnimalEventType EventType, DateTimeOffset EventDate,
    Guid? WorkerId, decimal? Cost, string? Description,
    decimal? Amount, Guid? CategoryId,
    OffspringData? Offspring = null) : IRequest<AnimalEventDto>;

public sealed class CreateAnimalEventHandler : IRequestHandler<CreateAnimalEventCommand, AnimalEventDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateAnimalEventHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<AnimalEventDto> Handle(CreateAnimalEventCommand r, CancellationToken ct)
    {
        var a = await _db.Animals
            .FirstOrDefaultAsync(a => a.Id == r.AnimalId && a.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Animal {r.AnimalId} no encontrado.");

        // Update current weight when a weight-recording event is submitted
        if (r.EventType == AnimalEventType.RegistroPeso && r.Amount.HasValue && r.Amount > 0)
            a.Weight = r.Amount.Value;

        var ev = new AnimalEvent
        {
            TenantId        = _user.TenantId,
            AnimalId        = a.Id,
            EventType       = r.EventType,
            EventDate       = r.EventDate,
            FarmId          = a.FarmId,
            WorkerId        = r.WorkerId,
            Cost            = r.Cost ?? r.Amount,
            Description     = r.Description?.Trim(),
            CreatedByUserId = _user.UserId,
        };
        _db.AnimalEvents.Add(ev);

        // Birth event: create offspring animal atomically (REQ-EVENT-02)
        // MotherId = the current animal (mother), which is always Hembra in this flow.
        if (r.EventType == AnimalEventType.Nacimiento && r.Offspring is not null)
        {
            var o = r.Offspring;
            if (await _db.Animals.AnyAsync(x => x.TagNumber == o.TagNumber && x.TenantId == _user.TenantId, ct))
                throw new InvalidOperationException($"El número de arete '{o.TagNumber}' ya existe.");

            var offspring = new Animal
            {
                TenantId   = _user.TenantId,
                TagNumber  = o.TagNumber.Trim(),
                Name       = o.Name?.Trim(),
                Breed      = o.Breed?.Trim() ?? a.Breed,
                Sex        = o.Sex,
                BirthDate  = DateOnly.FromDateTime(r.EventDate.UtcDateTime),
                Weight     = o.Weight,
                FarmId     = a.FarmId,
                DivisionId = a.DivisionId,
                MotherId   = a.Id,   // the mother is the current animal
            };
            _db.Animals.Add(offspring);
        }

        // Atomic economy transaction for purchase/sale events
        if (r.Amount.HasValue && r.Amount > 0 &&
            (r.EventType == AnimalEventType.Compra || r.EventType == AnimalEventType.Venta))
        {
            var txnType = r.EventType == AnimalEventType.Compra
                ? TransactionType.Egreso
                : TransactionType.Ingreso;

            var catName = r.EventType == AnimalEventType.Compra ? "Compra de Ganado" : "Venta de Ganado";
            if (r.CategoryId.HasValue)
                catName = await _db.TransactionCategories
                    .Where(c => c.Id == r.CategoryId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync(ct) ?? catName;

            var txn = new EconomyTransaction
            {
                TenantId        = _user.TenantId,
                Type            = txnType,
                CategoryId      = r.CategoryId,
                CategoryName    = catName,
                Description     = r.Description?.Trim() ?? $"{r.EventType} animal {a.TagNumber}",
                Amount          = r.Amount.Value,
                TxnDate         = r.EventDate,
                FarmId          = a.FarmId,
                SourceEventId   = ev.Id,
                CreatedByUserId = _user.UserId,
            };
            _db.EconomyTransactions.Add(txn);
        }

        await _db.SaveChangesAsync(ct);
        return new AnimalEventDto(ev.Id, ev.AnimalId, ev.EventType, ev.EventDate,
            ev.FarmId, ev.WorkerId, ev.Cost, ev.Description, ev.CreatedAt);
    }
}

// ── Update animal event (mutable fields only) ─────────────────────────────────
public sealed record UpdateAnimalEventCommand(
    Guid AnimalId, Guid EventId,
    DateTimeOffset EventDate, Guid? WorkerId,
    decimal? Cost, string? Description) : IRequest<AnimalEventDto>;

public sealed class UpdateAnimalEventHandler : IRequestHandler<UpdateAnimalEventCommand, AnimalEventDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UpdateAnimalEventHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<AnimalEventDto> Handle(UpdateAnimalEventCommand r, CancellationToken ct)
    {
        var ev = await _db.AnimalEvents
            .Include(e => e.Animal)
            .FirstOrDefaultAsync(e => e.Id == r.EventId && e.AnimalId == r.AnimalId && e.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Evento {r.EventId} no encontrado.");

        ev.EventDate   = r.EventDate;
        ev.WorkerId    = r.WorkerId;
        ev.Cost        = r.Cost;
        ev.Description = r.Description?.Trim();

        // Re-sync animal weight if this is still the most recent weight event
        if (ev.EventType == AnimalEventType.RegistroPeso && r.Cost.HasValue && r.Cost > 0)
        {
            var latestWeightEventId = await _db.AnimalEvents
                .Where(e => e.AnimalId == r.AnimalId && e.EventType == AnimalEventType.RegistroPeso)
                .OrderByDescending(e => e.EventDate)
                .Select(e => e.Id)
                .FirstOrDefaultAsync(ct);
            if (latestWeightEventId == ev.Id)
                ev.Animal.Weight = r.Cost.Value;
        }

        await _db.SaveChangesAsync(ct);
        return new AnimalEventDto(ev.Id, ev.AnimalId, ev.EventType, ev.EventDate,
            ev.FarmId, ev.WorkerId, ev.Cost, ev.Description, ev.CreatedAt);
    }
}

// ── Delete animal event (with state re-evaluation) ────────────────────────────
public sealed record DeleteAnimalEventCommand(Guid AnimalId, Guid EventId) : IRequest<Unit>;

public sealed class DeleteAnimalEventHandler : IRequestHandler<DeleteAnimalEventCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public DeleteAnimalEventHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<Unit> Handle(DeleteAnimalEventCommand r, CancellationToken ct)
    {
        var ev = await _db.AnimalEvents
            .Include(e => e.Animal)
            .FirstOrDefaultAsync(e => e.Id == r.EventId && e.AnimalId == r.AnimalId && e.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Evento {r.EventId} no encontrado.");

        var eventType = ev.EventType;
        var animal    = ev.Animal;

        _db.AnimalEvents.Remove(ev);
        await _db.SaveChangesAsync(ct);

        // Muerte deleted → reactivate animal
        if (eventType == AnimalEventType.Muerte)
        {
            animal.Status       = AnimalStatus.Activo;
            animal.HealthStatus = AnimalHealthStatus.Sano;
            animal.CloseReason  = null;
            animal.ClosedAt     = null;
            await _db.SaveChangesAsync(ct);
        }

        // RegistroPeso deleted → recalculate weight from remaining weight events
        if (eventType == AnimalEventType.RegistroPeso)
        {
            var latestWeight = await _db.AnimalEvents
                .Where(e => e.AnimalId == r.AnimalId && e.EventType == AnimalEventType.RegistroPeso)
                .OrderByDescending(e => e.EventDate)
                .Select(e => (decimal?)e.Cost)
                .FirstOrDefaultAsync(ct);
            animal.Weight = latestWeight;
            await _db.SaveChangesAsync(ct);
        }

        return Unit.Value;
    }
}
