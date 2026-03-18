using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

/// <summary>
/// Persisted refresh token. Only the SHA-256 hash of the raw token is stored;
/// the raw value is sent to the client and never stored.
/// Supports secure rotation: each use revokes the old token and creates a new one.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>SHA-256 hash of the raw random token sent to the client.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Points to the token that replaced this one during rotation.
    /// Enables detection of refresh token reuse attacks.
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }

    // ── Computed helpers (not persisted) ──────────────────────────────────
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    public User User { get; set; } = null!;
}
