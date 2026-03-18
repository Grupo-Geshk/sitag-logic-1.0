using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

public class Division : TenantEntity
{
    public Guid FarmId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? MaxCapacity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? DeletedAt { get; set; }

    public Farm Farm { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
