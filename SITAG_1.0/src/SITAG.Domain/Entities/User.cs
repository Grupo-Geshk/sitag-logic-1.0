using SITAG.Domain.Common;
using SITAG.Domain.Enums;

namespace SITAG.Domain.Entities;

public class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = false;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    // ── Password reset ────────────────────────────────────────────────────────
    public string?          PasswordResetTokenHash      { get; set; }
    public DateTimeOffset?  PasswordResetTokenExpiresAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
