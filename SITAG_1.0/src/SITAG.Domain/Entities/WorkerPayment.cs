using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

public class WorkerPayment : TenantEntity
{
    public Guid WorkerId { get; set; }
    public PaymentMode Mode { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Rate { get; set; }          // rate per hour or per day
    public decimal Quantity { get; set; }      // hours worked or days worked
    public decimal TotalAmount { get; set; }   // Rate * Quantity
    public string? Notes { get; set; }
    public Guid? FarmId { get; set; }
    public Guid? TransactionId { get; set; }   // linked EconomyTransaction (auto-created)

    public Worker Worker { get; set; } = null!;
}
