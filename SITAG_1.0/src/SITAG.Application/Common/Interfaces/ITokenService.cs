using SITAG.Domain.Entities;

namespace SITAG.Application.Common.Interfaces;

public interface ITokenService
{
    /// <summary>Generates a signed JWT access token for the given user.</summary>
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically random refresh token.
    /// Returns the raw value (sent to client), its SHA-256 hash (stored in DB),
    /// and the absolute expiry timestamp.
    /// </summary>
    (string Raw, string Hash, DateTimeOffset ExpiresAt) GenerateRefreshToken();
}
