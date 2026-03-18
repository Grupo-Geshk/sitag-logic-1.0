using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

public class EconomyTransaction : TenantEntity
{
    public TransactionType Type { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }  // denormalized for MVP
    public string? Description { get; set; }
    public decimal Amount { get; set; }         // always positive
    public DateTimeOffset TxnDate { get; set; }
    public Guid? FarmId { get; set; }
    public Guid? SourceEventId { get; set; }    // optional link to AnimalEvent
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public TransactionCategory? Category { get; set; }
}
