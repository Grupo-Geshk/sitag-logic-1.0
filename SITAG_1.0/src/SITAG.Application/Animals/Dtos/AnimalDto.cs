using SITAG.Domain.Enums;

namespace SITAG.Application.Animals.Dtos;

// ── Timeline entry (events + movements merged) ────────────────────────────────
public sealed record AnimalTimelineEntryDto(
    string    EntryType,          // "EVENT" | "MOVEMENT"
    DateTimeOffset Date,
    string    Description,
    Guid?     EventId,
    AnimalEventType? EventType,
    Guid?     MovementId,
    MovementScope? MovementScope,
    Guid?     FromFarmId,
    string?   FromFarmName,
    Guid?     ToFarmId,
    string?   ToFarmName,
    decimal?  Cost);

// ── Animal list item ──────────────────────────────────────────────────────────
public sealed record AnimalDto(
    Guid Id,
    Guid TenantId,
    string TagNumber,
    string? Name,
    string? Breed,
    string Sex,
    DateOnly? BirthDate,
    decimal? Weight,
    AnimalStatus Status,
    AnimalHealthStatus HealthStatus,
    Guid FarmId,
    Guid? DivisionId,
    // ── Genealogy ─────────────────────────────────────────────────────────────
    Guid? MotherId,
    string? MotherRef,
    Guid? FatherId,
    string? FatherRef,
    // ── Photo ─────────────────────────────────────────────────────────────────
    string? PhotoUrl,
    // ── Backward compat alias (= MotherId) ────────────────────────────────────
    Guid? ParentId,
    // ── Closure ───────────────────────────────────────────────────────────────
    string? CloseReason,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt,
    string? FarmName = null,
    string? DivisionName = null,
    string? Color = null);

// ── Animal detail (single animal view) ───────────────────────────────────────
public sealed record AnimalDetailDto(
    Guid Id,
    Guid TenantId,
    string TagNumber,
    string? Name,
    string? Breed,
    string Sex,
    DateOnly? BirthDate,
    decimal? CurrentWeight,
    AnimalStatus Status,
    AnimalHealthStatus HealthStatus,
    Guid FarmId,
    string? FarmName,
    Guid? DivisionId,
    string? DivisionName,
    // ── Genealogy (enriched with resolved names for display) ──────────────────
    Guid? MotherId,
    string? MotherTagNumber,
    string? MotherName,
    string? MotherRef,
    Guid? FatherId,
    string? FatherTagNumber,
    string? FatherName,
    string? FatherRef,
    // ── Photo ─────────────────────────────────────────────────────────────────
    string? PhotoUrl,
    // ── Backward compat alias (= MotherId) ────────────────────────────────────
    Guid? ParentId,
    // ── Closure & audit ───────────────────────────────────────────────────────
    string? CloseReason,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt,
    // ── Offspring stats ───────────────────────────────────────────────────────
    int OffspringCount,
    DateOnly? LastBirthDate,
    string? Color = null);

// ── Single animal for genealogy tree nodes ───────────────────────────────────
public sealed record AnimalLineageDto(
    Guid Id,
    string TagNumber,
    string? Name,
    string Sex,
    string? Breed,
    DateOnly? BirthDate,
    AnimalStatus Status,
    string? PhotoUrl);

// ── Full genealogy tree (up to 3 generations + offspring) ────────────────────
public sealed record AnimalGenealogyDto(
    AnimalLineageDto Self,
    // ── Parents ───────────────────────────────────────────────────────────────
    AnimalLineageDto? Mother,
    string? MotherRef,
    AnimalLineageDto? Father,
    string? FatherRef,
    // ── Maternal grandparents ─────────────────────────────────────────────────
    AnimalLineageDto? MaternalGrandmother,
    string? MaternalGrandmotherRef,
    AnimalLineageDto? MaternalGrandfather,
    string? MaternalGrandfatherRef,
    // ── Paternal grandparents ─────────────────────────────────────────────────
    AnimalLineageDto? PaternalGrandmother,
    string? PaternalGrandmotherRef,
    AnimalLineageDto? PaternalGrandfather,
    string? PaternalGrandfatherRef,
    // ── Offspring (all children registered in the system) ─────────────────────
    IReadOnlyList<AnimalLineageDto> Offspring);

// ── Event ─────────────────────────────────────────────────────────────────────
public sealed record AnimalEventDto(
    Guid Id,
    Guid AnimalId,
    AnimalEventType EventType,
    DateTimeOffset EventDate,
    Guid FarmId,
    Guid? WorkerId,
    decimal? Cost,
    string? Description,
    DateTimeOffset CreatedAt);

// ── Movement ──────────────────────────────────────────────────────────────────
public sealed record AnimalMovementDto(
    Guid Id,
    Guid AnimalId,
    Guid? FromFarmId,
    Guid? FromDivisionId,
    Guid ToFarmId,
    Guid? ToDivisionId,
    DateTimeOffset MovementDate,
    MovementScope Scope,
    string? Reason,
    DateTimeOffset CreatedAt);
