using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

public class ImportDraft : TenantEntity
{
    public string FileName { get; set; } = string.Empty;
    public ImportDraftStatus Status { get; set; } = ImportDraftStatus.Pending;

    // JSON array of ImportRowDto — opaque to the domain, typed in Application layer
    public string RowsJson { get; set; } = "[]";

    public DateTimeOffset ExpiresAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
