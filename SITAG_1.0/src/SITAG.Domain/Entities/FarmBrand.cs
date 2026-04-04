using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

/// <summary>
/// Represents a livestock branding iron registered for a farm.
/// Tracks the brand mark (name + photo) used to identify animal ownership.
/// </summary>
public class FarmBrand : TenantEntity
{
    public Guid FarmId { get; set; }

    /// <summary>Reference name, e.g. "Hierro N°3 – Rancho El Cedro"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>External URL of the brand mark photo (IMGBB or similar).</summary>
    public string? PhotoUrl { get; set; }

    public Farm Farm { get; set; } = null!;
    public ICollection<Animal> Animals { get; set; } = [];
}
