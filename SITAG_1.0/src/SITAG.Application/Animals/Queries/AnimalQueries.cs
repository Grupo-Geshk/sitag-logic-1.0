using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Animals.Dtos;
using SITAG.Application.Common.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Enums;

namespace SITAG.Application.Animals.Queries;

// ── List with filters ─────────────────────────────────────────────────────────
public sealed record GetAnimalsQuery(
    Guid?              FarmId,
    Guid?              DivisionId,
    AnimalStatus?      Status,
    string?            Sex,
    AnimalHealthStatus? HealthStatus,
    int?               AgeMinMonths,
    int?               AgeMaxMonths,
    string?            Search,
    int PageNumber = 1,
    int PageSize   = 20) : IRequest<PagedResult<AnimalDto>>;

public sealed class GetAnimalsHandler : IRequestHandler<GetAnimalsQuery, PagedResult<AnimalDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetAnimalsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<PagedResult<AnimalDto>> Handle(GetAnimalsQuery r, CancellationToken ct)
    {
        var query = _db.Animals.AsNoTracking().Where(a => a.TenantId == _user.TenantId);

        if (r.FarmId.HasValue)       query = query.Where(a => a.FarmId == r.FarmId);
        if (r.DivisionId.HasValue)   query = query.Where(a => a.DivisionId == r.DivisionId);
        if (r.Status.HasValue)       query = query.Where(a => a.Status == r.Status);
        if (!string.IsNullOrWhiteSpace(r.Sex)) query = query.Where(a => a.Sex == r.Sex);
        if (r.HealthStatus.HasValue) query = query.Where(a => a.HealthStatus == r.HealthStatus);

        if (r.AgeMinMonths.HasValue)
        {
            var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-r.AgeMinMonths.Value));
            query = query.Where(a => a.BirthDate == null || a.BirthDate <= cutoff);
        }
        if (r.AgeMaxMonths.HasValue)
        {
            var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-r.AgeMaxMonths.Value));
            query = query.Where(a => a.BirthDate == null || a.BirthDate >= cutoff);
        }
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var s = r.Search.Trim().ToLower();
            query = query.Where(a => a.TagNumber.ToLower().Contains(s) ||
                                     (a.Name != null && a.Name.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((r.PageNumber - 1) * r.PageSize)
            .Take(r.PageSize)
            .Select(a => new AnimalDto(
                a.Id, a.TenantId, a.TagNumber, a.Name, a.Breed, a.Sex,
                a.BirthDate, a.Weight, a.Status, a.HealthStatus,
                a.FarmId, a.DivisionId,
                a.MotherId, a.MotherRef,
                a.FatherId, a.FatherRef,
                a.PhotoUrl,
                a.MotherId,   // ParentId compat alias — positional, named args not allowed in expression trees
                a.CloseReason, a.ClosedAt, a.CreatedAt,
                _db.Farms.Where(f => f.Id == a.FarmId).Select(f => f.Name).FirstOrDefault(),
                a.DivisionId != null
                    ? _db.Divisions.Where(d => d.Id == a.DivisionId).Select(d => d.Name).FirstOrDefault()
                    : null,
                a.Color))
            .ToListAsync(ct);

        return new PagedResult<AnimalDto>(items, total, r.PageNumber, r.PageSize);
    }
}

// ── By ID (enriched detail with resolved parent names) ───────────────────────
public sealed record GetAnimalByIdQuery(Guid AnimalId) : IRequest<AnimalDetailDto>;

public sealed class GetAnimalByIdHandler : IRequestHandler<GetAnimalByIdQuery, AnimalDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetAnimalByIdHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<AnimalDetailDto> Handle(GetAnimalByIdQuery r, CancellationToken ct)
    {
        var a = await _db.Animals
            .AsNoTracking()
            .Where(a => a.Id == r.AnimalId && a.TenantId == _user.TenantId)
            .Select(a => new AnimalDetailDto(
                a.Id, a.TenantId, a.TagNumber, a.Name, a.Breed, a.Sex,
                a.BirthDate, a.Weight, a.Status, a.HealthStatus,
                a.FarmId,
                _db.Farms.Where(f => f.Id == a.FarmId).Select(f => f.Name).FirstOrDefault(),
                a.DivisionId,
                a.DivisionId != null
                    ? _db.Divisions.Where(d => d.Id == a.DivisionId).Select(d => d.Name).FirstOrDefault()
                    : null,
                // Genealogy — MotherId
                a.MotherId,
                a.MotherId != null
                    ? _db.Animals.Where(m => m.Id == a.MotherId).Select(m => m.TagNumber).FirstOrDefault()
                    : null,
                a.MotherId != null
                    ? _db.Animals.Where(m => m.Id == a.MotherId).Select(m => m.Name).FirstOrDefault()
                    : null,
                a.MotherRef,
                // Genealogy — FatherId
                a.FatherId,
                a.FatherId != null
                    ? _db.Animals.Where(f => f.Id == a.FatherId).Select(f => f.TagNumber).FirstOrDefault()
                    : null,
                a.FatherId != null
                    ? _db.Animals.Where(f => f.Id == a.FatherId).Select(f => f.Name).FirstOrDefault()
                    : null,
                a.FatherRef,
                // Photo
                a.PhotoUrl,
                // ParentId compat alias — positional, named args not allowed in expression trees
                a.MotherId,
                a.CloseReason, a.ClosedAt, a.CreatedAt,
                // Offspring count: all animals where this animal is mother OR father
                _db.Animals.Count(c => c.MotherId == a.Id || c.FatherId == a.Id),
                // LastBirthDate: from maternal children only (birth is tracked via mother)
                _db.Animals
                    .Where(c => c.MotherId == a.Id && c.BirthDate != null)
                    .OrderByDescending(c => c.BirthDate)
                    .Select(c => c.BirthDate)
                    .FirstOrDefault(),
                a.Color))
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Animal {r.AnimalId} no encontrado.");
        return a;
    }
}

// ── Genealogy tree (up to 3 generations + offspring) ─────────────────────────
public sealed record GetAnimalGenealogyQuery(Guid AnimalId) : IRequest<AnimalGenealogyDto>;

public sealed class GetAnimalGenealogyHandler : IRequestHandler<GetAnimalGenealogyQuery, AnimalGenealogyDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetAnimalGenealogyHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<AnimalGenealogyDto> Handle(GetAnimalGenealogyQuery r, CancellationToken ct)
    {
        var tid = _user.TenantId;

        // ── Self ──────────────────────────────────────────────────────────────
        var self = await _db.Animals.AsNoTracking()
            .Where(a => a.Id == r.AnimalId && a.TenantId == tid)
            .Select(a => new
            {
                a.Id, a.TagNumber, a.Name, a.Sex, a.Breed, a.BirthDate, a.Status, a.PhotoUrl,
                a.MotherId, a.MotherRef, a.FatherId, a.FatherRef
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Animal {r.AnimalId} no encontrado.");

        static AnimalLineageDto ToLineage(
            Guid id, string tag, string? name, string sex,
            string? breed, DateOnly? birth, AnimalStatus status, string? photo) =>
            new(id, tag, name, sex, breed, birth, status, photo);

        // ── Parents (resolve from DB if linked) ───────────────────────────────
        AnimalLineageDto? mother = null;
        AnimalLineageDto? father = null;
        Guid? maternGmId = null; string? maternGmRef = null;
        Guid? maternGfId = null; string? maternGfRef = null;
        Guid? paternGmId = null; string? paternGmRef = null;
        Guid? paternGfId = null; string? paternGfRef = null;

        if (self.MotherId.HasValue)
        {
            var m = await _db.Animals.AsNoTracking()
                .Where(a => a.Id == self.MotherId && a.TenantId == tid)
                .Select(a => new { a.Id, a.TagNumber, a.Name, a.Sex, a.Breed, a.BirthDate, a.Status, a.PhotoUrl,
                    a.MotherId, a.MotherRef, a.FatherId, a.FatherRef })
                .FirstOrDefaultAsync(ct);
            if (m != null)
            {
                mother = ToLineage(m.Id, m.TagNumber, m.Name, m.Sex, m.Breed, m.BirthDate, m.Status, m.PhotoUrl);
                maternGmId  = m.MotherId;  maternGmRef = m.MotherRef;
                maternGfId  = m.FatherId;  maternGfRef = m.FatherRef;
            }
        }

        if (self.FatherId.HasValue)
        {
            var f = await _db.Animals.AsNoTracking()
                .Where(a => a.Id == self.FatherId && a.TenantId == tid)
                .Select(a => new { a.Id, a.TagNumber, a.Name, a.Sex, a.Breed, a.BirthDate, a.Status, a.PhotoUrl,
                    a.MotherId, a.MotherRef, a.FatherId, a.FatherRef })
                .FirstOrDefaultAsync(ct);
            if (f != null)
            {
                father = ToLineage(f.Id, f.TagNumber, f.Name, f.Sex, f.Breed, f.BirthDate, f.Status, f.PhotoUrl);
                paternGmId  = f.MotherId;  paternGmRef = f.MotherRef;
                paternGfId  = f.FatherId;  paternGfRef = f.FatherRef;
            }
        }

        // ── Grandparents (resolve FKs if available) ───────────────────────────
        async Task<AnimalLineageDto?> ResolveAsync(Guid? id)
        {
            if (!id.HasValue) return null;
            return await _db.Animals.AsNoTracking()
                .Where(a => a.Id == id && a.TenantId == tid)
                .Select(a => new AnimalLineageDto(a.Id, a.TagNumber, a.Name, a.Sex, a.Breed, a.BirthDate, a.Status, a.PhotoUrl))
                .FirstOrDefaultAsync(ct);
        }

        var maternGm = await ResolveAsync(maternGmId);
        var maternGf = await ResolveAsync(maternGfId);
        var paternGm = await ResolveAsync(paternGmId);
        var paternGf = await ResolveAsync(paternGfId);

        // ── Offspring (all children where this animal is mother or father) ────
        var offspring = await _db.Animals.AsNoTracking()
            .Where(a => (a.MotherId == r.AnimalId || a.FatherId == r.AnimalId) && a.TenantId == tid)
            .OrderByDescending(a => a.BirthDate)
            .Select(a => new AnimalLineageDto(a.Id, a.TagNumber, a.Name, a.Sex, a.Breed, a.BirthDate, a.Status, a.PhotoUrl))
            .ToListAsync(ct);

        return new AnimalGenealogyDto(
            Self:                    ToLineage(self.Id, self.TagNumber, self.Name, self.Sex, self.Breed, self.BirthDate, self.Status, self.PhotoUrl),
            Mother:                  mother,
            MotherRef:               self.MotherId.HasValue ? null : self.MotherRef,  // show ref only when no FK
            Father:                  father,
            FatherRef:               self.FatherId.HasValue ? null : self.FatherRef,
            MaternalGrandmother:     maternGm,
            MaternalGrandmotherRef:  maternGm is null ? maternGmRef : null,
            MaternalGrandfather:     maternGf,
            MaternalGrandfatherRef:  maternGf is null ? maternGfRef : null,
            PaternalGrandmother:     paternGm,
            PaternalGrandmotherRef:  paternGm is null ? paternGmRef : null,
            PaternalGrandfather:     paternGf,
            PaternalGrandfatherRef:  paternGf is null ? paternGfRef : null,
            Offspring:               offspring);
    }
}

// ── Animal events (single animal) ─────────────────────────────────────────────
public sealed record GetAnimalEventsQuery(Guid AnimalId) : IRequest<IReadOnlyList<AnimalEventDto>>;

public sealed class GetAnimalEventsHandler : IRequestHandler<GetAnimalEventsQuery, IReadOnlyList<AnimalEventDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetAnimalEventsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<AnimalEventDto>> Handle(GetAnimalEventsQuery r, CancellationToken ct)
    {
        var exists = await _db.Animals
            .AnyAsync(a => a.Id == r.AnimalId && a.TenantId == _user.TenantId, ct);
        if (!exists) throw new KeyNotFoundException($"Animal {r.AnimalId} no encontrado.");

        return await _db.AnimalEvents
            .AsNoTracking()
            .Where(e => e.AnimalId == r.AnimalId && e.TenantId == _user.TenantId)
            .OrderByDescending(e => e.EventDate)
            .Select(e => new AnimalEventDto(e.Id, e.AnimalId, e.EventType, e.EventDate,
                e.FarmId, e.WorkerId, e.Cost, e.Description, e.CreatedAt))
            .ToListAsync(ct);
    }
}

// ── Cross-animal events query (with filters) ──────────────────────────────────
public sealed record GetAnimalEventsFilteredQuery(
    Guid?            FarmId,
    Guid?            AnimalId,
    AnimalEventType? EventType,
    DateTimeOffset?  StartDate,
    DateTimeOffset?  EndDate,
    int PageNumber = 1,
    int PageSize   = 20) : IRequest<PagedResult<AnimalEventDto>>;

public sealed class GetAnimalEventsFilteredHandler
    : IRequestHandler<GetAnimalEventsFilteredQuery, PagedResult<AnimalEventDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetAnimalEventsFilteredHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<PagedResult<AnimalEventDto>> Handle(GetAnimalEventsFilteredQuery r, CancellationToken ct)
    {
        var query = _db.AnimalEvents
            .AsNoTracking()
            .Where(e => e.TenantId == _user.TenantId);

        if (r.AnimalId.HasValue)  query = query.Where(e => e.AnimalId == r.AnimalId);
        if (r.FarmId.HasValue)    query = query.Where(e => e.FarmId == r.FarmId);
        if (r.EventType.HasValue) query = query.Where(e => e.EventType == r.EventType);
        if (r.StartDate.HasValue) query = query.Where(e => e.EventDate >= r.StartDate);
        if (r.EndDate.HasValue)   query = query.Where(e => e.EventDate <= r.EndDate);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.EventDate)
            .Skip((r.PageNumber - 1) * r.PageSize)
            .Take(r.PageSize)
            .Select(e => new AnimalEventDto(e.Id, e.AnimalId, e.EventType, e.EventDate,
                e.FarmId, e.WorkerId, e.Cost, e.Description, e.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AnimalEventDto>(items, total, r.PageNumber, r.PageSize);
    }
}

// ── Animal movements ──────────────────────────────────────────────────────────
public sealed record GetAnimalMovementsQuery(Guid AnimalId) : IRequest<IReadOnlyList<AnimalMovementDto>>;

public sealed class GetAnimalMovementsHandler
    : IRequestHandler<GetAnimalMovementsQuery, IReadOnlyList<AnimalMovementDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetAnimalMovementsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<AnimalMovementDto>> Handle(GetAnimalMovementsQuery r, CancellationToken ct)
    {
        var exists = await _db.Animals
            .AnyAsync(a => a.Id == r.AnimalId && a.TenantId == _user.TenantId, ct);
        if (!exists) throw new KeyNotFoundException($"Animal {r.AnimalId} no encontrado.");

        return await _db.AnimalMovements
            .AsNoTracking()
            .Where(m => m.AnimalId == r.AnimalId && m.TenantId == _user.TenantId)
            .OrderByDescending(m => m.MovementDate)
            .Select(m => new AnimalMovementDto(m.Id, m.AnimalId, m.FromFarmId, m.FromDivisionId,
                m.ToFarmId, m.ToDivisionId, m.MovementDate, m.Scope, m.Reason, m.CreatedAt))
            .ToListAsync(ct);
    }
}

// ── Cross-animal movements query (with filters) ────────────────────────────────
public sealed record GetMovementsQuery(
    Guid?            FarmId,
    Guid?            AnimalId,
    DateTimeOffset?  StartDate,
    DateTimeOffset?  EndDate,
    int PageNumber = 1,
    int PageSize   = 20) : IRequest<PagedResult<AnimalMovementDto>>;

public sealed class GetMovementsHandler : IRequestHandler<GetMovementsQuery, PagedResult<AnimalMovementDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetMovementsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<PagedResult<AnimalMovementDto>> Handle(GetMovementsQuery r, CancellationToken ct)
    {
        var query = _db.AnimalMovements
            .AsNoTracking()
            .Where(m => m.TenantId == _user.TenantId);

        if (r.AnimalId.HasValue)  query = query.Where(m => m.AnimalId == r.AnimalId);
        if (r.FarmId.HasValue)    query = query.Where(m => m.FromFarmId == r.FarmId || m.ToFarmId == r.FarmId);
        if (r.StartDate.HasValue) query = query.Where(m => m.MovementDate >= r.StartDate);
        if (r.EndDate.HasValue)   query = query.Where(m => m.MovementDate <= r.EndDate);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.MovementDate)
            .Skip((r.PageNumber - 1) * r.PageSize)
            .Take(r.PageSize)
            .Select(m => new AnimalMovementDto(m.Id, m.AnimalId, m.FromFarmId, m.FromDivisionId,
                m.ToFarmId, m.ToDivisionId, m.MovementDate, m.Scope, m.Reason, m.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AnimalMovementDto>(items, total, r.PageNumber, r.PageSize);
    }
}

// ── Animal timeline (events + movements merged, chronological) ─────────────────
public sealed record GetAnimalTimelineQuery(Guid AnimalId)
    : IRequest<IReadOnlyList<AnimalTimelineEntryDto>>;

public sealed class GetAnimalTimelineHandler
    : IRequestHandler<GetAnimalTimelineQuery, IReadOnlyList<AnimalTimelineEntryDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetAnimalTimelineHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<AnimalTimelineEntryDto>> Handle(GetAnimalTimelineQuery r, CancellationToken ct)
    {
        var tid = _user.TenantId;
        var exists = await _db.Animals.AnyAsync(a => a.Id == r.AnimalId && a.TenantId == tid, ct);
        if (!exists) throw new KeyNotFoundException($"Animal {r.AnimalId} no encontrado.");

        var events = await _db.AnimalEvents.AsNoTracking()
            .Where(e => e.AnimalId == r.AnimalId && e.TenantId == tid)
            .Select(e => new AnimalTimelineEntryDto(
                "EVENT", e.EventDate,
                e.Description ?? e.EventType.ToString(),
                e.Id, e.EventType,
                null, null, null, null, null, null,
                e.Cost))
            .ToListAsync(ct);

        var movements = await _db.AnimalMovements.AsNoTracking()
            .Where(m => m.AnimalId == r.AnimalId && m.TenantId == tid)
            .Select(m => new AnimalTimelineEntryDto(
                "MOVEMENT", m.MovementDate,
                m.Reason ?? $"Movimiento {m.Scope}",
                null, null,
                m.Id, m.Scope,
                m.FromFarmId,
                m.FromFarmId != null ? _db.Farms.Where(f => f.Id == m.FromFarmId).Select(f => f.Name).FirstOrDefault() : null,
                m.ToFarmId,
                _db.Farms.Where(f => f.Id == m.ToFarmId).Select(f => f.Name).FirstOrDefault(),
                null))
            .ToListAsync(ct);

        return events.Concat(movements)
            .OrderByDescending(e => e.Date)
            .ToList();
    }
}
