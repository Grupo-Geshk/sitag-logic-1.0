using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

/// <summary>
/// N:M link between a service and the animals it targets (DATABASE_MODEL.md §10.2).
/// (ServiceId, AnimalId) must be unique.
/// </summary>
public class ServiceAnimal : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid AnimalId { get; set; }

    public VetService Service { get; set; } = null!;
    public Animal Animal { get; set; } = null!;
}
