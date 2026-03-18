using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

/// <summary>
/// Append-only event history (vaccination, weight, treatment, etc.) (DATABASE_MODEL.md §7.2).
/// Events do not change animal location; movements do.
/// </summary>
public class AnimalEvent : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid AnimalId { get; set; }
    public AnimalEventType EventType { get; set; }
    public DateTimeOffset EventDate { get; set; }
    public Guid FarmId { get; set; }
    public Guid? WorkerId { get; set; }
    public decimal? Cost { get; set; }
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }

    public Animal Animal { get; set; } = null!;
    public Worker? Worker { get; set; }
}
