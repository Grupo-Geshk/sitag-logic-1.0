namespace SITAG.Infrastructure.Identity;

/// <summary>
/// Bound from the JWT__ environment variable group (JWT__Issuer, JWT__Audience, etc.).
/// All values are required at startup; the DI extension will throw if any is missing.
/// </summary>
public sealed class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Lifetime of the access token in minutes. Default: 15.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Lifetime of the refresh token in days. Default: 30.</summary>
    public int RefreshTokenDays { get; set; } = 30;
}
