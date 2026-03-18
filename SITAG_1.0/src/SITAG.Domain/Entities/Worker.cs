using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

public class Worker : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? RoleLabel { get; set; }
    public string? Contact { get; set; }
    public WorkerStatus Status { get; set; } = WorkerStatus.Activo;
    public DateTimeOffset? DeletedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<WorkerFarmAssignment> FarmAssignments { get; set; } = [];
}
