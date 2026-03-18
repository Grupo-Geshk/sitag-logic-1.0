using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

/// <summary>
/// Explicit ledger of supplies consumed by a service (DATABASE_MODEL.md §10.3).
/// Consuming supplies must also create SupplyMovement entries atomically.
/// </summary>
public class ServiceSupplyConsumption : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid SupplyId { get; set; }
    public decimal Quantity { get; set; }

    public VetService Service { get; set; } = null!;
    public Supply Supply { get; set; } = null!;
}
