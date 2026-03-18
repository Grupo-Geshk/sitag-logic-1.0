using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

/// <summary>
/// N:M + History: a worker can be assigned to many farms over time (DATABASE_MODEL.md §8.2).
/// EndDate = null means the assignment is currently active.
/// Do not overwrite — close with EndDate and create a new row.
/// </summary>
public class WorkerFarmAssignment : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid WorkerId { get; set; }
    public Guid FarmId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid CreatedByUserId { get; set; }

    public Worker Worker { get; set; } = null!;
    public Farm Farm { get; set; } = null!;
}
