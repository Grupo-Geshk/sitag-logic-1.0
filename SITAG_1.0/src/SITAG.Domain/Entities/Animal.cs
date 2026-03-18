using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

public class Animal : TenantEntity
{
    public string TagNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Breed { get; set; }
    public string Sex { get; set; } = string.Empty;   // "Hembra" | "Macho"
    public DateOnly? BirthDate { get; set; }
    public decimal? Weight { get; set; }
    public AnimalStatus Status { get; set; } = AnimalStatus.Activo;
    public AnimalHealthStatus HealthStatus { get; set; } = AnimalHealthStatus.Sano;

    // Current location — kept in sync with AnimalMovement (atomic update)
    public Guid FarmId { get; set; }
    public Guid? DivisionId { get; set; }

    // ── Genealogy ─────────────────────────────────────────────────────────────
    // MotherId / FatherId: FK to an Animal record in this tenant (in-system parent).
    // MotherRef / FatherRef: free-text reference when parent is not in the system
    //   (e.g. "BOV-EXT-99 — Brahman adquirida externamente").
    // Both FK and Ref can coexist (FK is authoritative; Ref is informational).
    // Immutable post-creation (genealogy should not change after registration).
    public Guid? MotherId { get; set; }
    public string? MotherRef { get; set; }  // max 200 chars

    public Guid? FatherId { get; set; }
    public string? FatherRef { get; set; }  // max 200 chars

    // ── Current photo ─────────────────────────────────────────────────────────
    // External URL (IMGBB or similar). Never store binary here.
    // Mutable: can be updated at any time via PUT /animals/{id}.
    public string? PhotoUrl { get; set; }   // max 1000 chars

    public DateTimeOffset? ClosedAt { get; set; }
    public string? CloseReason { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Farm Farm { get; set; } = null!;
    public Division? Division { get; set; }
    public Animal? Mother { get; set; }
    public Animal? Father { get; set; }
    public ICollection<AnimalMovement> Movements { get; set; } = [];
    public ICollection<AnimalEvent> Events { get; set; } = [];
    public ICollection<ServiceAnimal> ServiceAnimals { get; set; } = [];
}
