using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

public class TransactionCategory : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public bool IsActive { get; set; } = true;

    public Tenant Tenant { get; set; } = null!;
    public ICollection<EconomyTransaction> Transactions { get; set; } = [];
}
