using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

/// <summary>
/// Append-only inventory ledger (DATABASE_MODEL.md §9.2).
/// Insert + update Supply.CurrentQuantity must be atomic.
/// </summary>
public class SupplyMovement : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SupplyId { get; set; }
    public SupplyMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal PreviousQuantity { get; set; }
    public decimal NewQuantity { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset MovementDate { get; set; }
    public Guid UserId { get; set; }

    public Supply Supply { get; set; } = null!;
}
