using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string PrimaryEmail { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public TenantPlan Plan { get; set; } = TenantPlan.Semilla;
    public DateTimeOffset? PaidUntil { get; set; }
    public string? Notes { get; set; }

    public ICollection<User> Users { get; set; } = [];
    public Producer? Producer { get; set; }
    public ICollection<TenantAuditLog> AuditLogs { get; set; } = [];
}
