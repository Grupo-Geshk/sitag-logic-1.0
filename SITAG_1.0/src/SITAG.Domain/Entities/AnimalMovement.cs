using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

/// <summary>
/// Append-only location history for an animal (DATABASE_MODEL.md §7.1).
/// Creating a movement must also update Animal.FarmId/DivisionId in the same transaction.
/// </summary>
public class AnimalMovement : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid AnimalId { get; set; }

    public Guid? FromFarmId { get; set; }
    public Guid? FromDivisionId { get; set; }
    public Guid ToFarmId { get; set; }
    public Guid? ToDivisionId { get; set; }

    public DateTimeOffset MovementDate { get; set; }
    public MovementScope Scope { get; set; }
    public string? Reason { get; set; }
    public Guid CreatedByUserId { get; set; }

    public Animal Animal { get; set; } = null!;
}
