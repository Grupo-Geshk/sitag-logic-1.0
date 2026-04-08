using SITAG.Domain.Common;

namespace SITAG.Domain.Entities;

/// <summary>
/// Represents a pending invitation for a user to join an existing tenant.
/// The invite token is single-use and expires after 72 hours.
/// Once accepted, AcceptedAt is stamped and the resulting UserId is linked.
/// </summary>
public class UserInvite : TenantEntity
{
    /// <summary>Email address the invite is intended for.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the raw token sent to the invitee. Never store the raw value.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Null until the invite is accepted.</summary>
    public DateTimeOffset? AcceptedAt { get; set; }

    /// <summary>The user created when the invite was accepted. Null until accepted.</summary>
    public Guid? AcceptedByUserId { get; set; }

    /// <summary>Admin user who created this invite.</summary>
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;

    // Computed helpers (not persisted)
    public bool IsExpired  => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsAccepted => AcceptedAt.HasValue;
    public bool IsValid    => !IsExpired && !IsAccepted;
}
