using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

/// <summary>
/// Represents a livestock branding iron registered for a producer (tenant-level).
/// </summary>
public class FarmBrand : TenantEntity
{
    /// <summary>Reference name, e.g. "Hierro Principal"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>External URL of the brand mark photo (IMGBB or similar).</summary>
    public string? PhotoUrl { get; set; }

    public ICollection<Animal> Animals { get; set; } = [];
}
