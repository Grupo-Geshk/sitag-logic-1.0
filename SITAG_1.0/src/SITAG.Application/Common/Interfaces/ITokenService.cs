using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Common.Interfaces;

public interface ITokenService
{
    /// <summary>Generates a signed JWT access token for the given user and tenant plan.</summary>
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(User user, TenantPlan plan);

    /// <summary>
    /// Generates a cryptographically random refresh token.
    /// Returns the raw value (sent to client), its SHA-256 hash (stored in DB),
    /// and the absolute expiry timestamp.
    /// </summary>
    (string Raw, string Hash, DateTimeOffset ExpiresAt) GenerateRefreshToken();
}
