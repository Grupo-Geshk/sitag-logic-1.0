using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

/// <summary>
/// Operational profile of a tenant. MVP: 1 Producer per Tenant (DATABASE_MODEL.md §4.1).
/// </summary>
public class Producer : BaseEntity
{
    public Guid TenantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public Tenant Tenant { get; set; } = null!;
}
