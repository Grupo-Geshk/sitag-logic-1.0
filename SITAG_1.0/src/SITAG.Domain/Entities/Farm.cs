using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

public class Farm : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public decimal? Hectares { get; set; }
    public string? FarmType { get; set; }   // "Ganadería" | "Mixta" | "Engorde"
    public bool IsOwned { get; set; } = true;
    public DateTimeOffset? DeletedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<Division> Divisions { get; set; } = [];
    public ICollection<Animal> Animals { get; set; } = [];
    public ICollection<FarmBrand> Brands { get; set; } = [];
}
