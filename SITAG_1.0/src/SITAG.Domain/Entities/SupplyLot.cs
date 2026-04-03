using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

/// <summary>
/// Represents a single purchase batch of a supply product.
/// Each lot tracks its own cost, supplier, expiration, and remaining quantity.
/// Multiple service events can consume from the same active lot.
/// </summary>
public class SupplyLot : TenantEntity
{
    public Guid SupplyId { get; set; }
    public decimal InitialQuantity { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Supplier { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public SupplyLotStatus Status { get; set; } = SupplyLotStatus.EnStock;
    public string? Notes { get; set; }

    public Supply Supply { get; set; } = null!;
    public ICollection<SupplyMovement> Movements { get; set; } = [];
}
