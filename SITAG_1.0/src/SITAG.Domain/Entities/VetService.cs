using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

/// <summary>
/// Scheduled or completed operational activity (DATABASE_MODEL.md §10.1).
/// Named VetService to avoid collision with System.ServiceProcess.ServiceBase.
/// </summary>
public class VetService : TenantEntity
{
    public string ServiceType { get; set; } = string.Empty;
    public ServiceStatus Status { get; set; } = ServiceStatus.Pendiente;
    public DateTimeOffset ScheduledDate { get; set; }
    public DateTimeOffset? CompletedDate { get; set; }
    public Guid FarmId { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid? WorkerId { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Farm Farm { get; set; } = null!;
    public Worker? Worker { get; set; }
    public ICollection<ServiceAnimal> ServiceAnimals { get; set; } = [];
    public ICollection<ServiceSupplyConsumption> SupplyConsumptions { get; set; } = [];
}
