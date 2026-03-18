using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

public class TenantAuditLog : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public TenantAuditAction Action { get; set; }
    public TenantStatus? FromStatus { get; set; }
    public TenantStatus? ToStatus { get; set; }
    public DateTimeOffset? PaidUntil { get; set; }
    public string? Note { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
