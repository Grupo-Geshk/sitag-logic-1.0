using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

public class WorkerLoan : TenantEntity
{
    public Guid WorkerId { get; set; }
    public decimal Amount { get; set; }           // original loan amount
    public decimal RemainingAmount { get; set; }  // decreases as payments are made
    public DateOnly LoanDate { get; set; }
    public string? Description { get; set; }

    public Worker Worker { get; set; } = null!;
}
