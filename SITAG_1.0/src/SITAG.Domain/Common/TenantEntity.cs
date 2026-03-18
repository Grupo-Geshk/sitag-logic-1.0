namespace SITAG.Domain.Common;

/// <summary>
/// Base class for all tenant-scoped operational entities.
/// Every operational record MUST belong to exactly one tenant (DATABASE_MODEL.md §2.1).
/// tenant_id is derived from the authenticated JWT context — never accepted from clients.
/// </summary>
public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
