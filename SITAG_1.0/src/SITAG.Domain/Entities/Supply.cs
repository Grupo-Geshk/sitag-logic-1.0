using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

public class Supply : TenantEntity
{
    public Guid? FarmId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal MinStockLevel { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Farm? Farm { get; set; }
    public ICollection<SupplyMovement> Movements { get; set; } = [];
}
