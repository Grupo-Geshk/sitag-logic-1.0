using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Infrastructure.Identity;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(User user, TenantPlan plan)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tenantId", user.TenantId.ToString()),
            new Claim("role", user.Role.ToString()),
            new Claim("plan", plan.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string Raw, string Hash, DateTimeOffset ExpiresAt) GenerateRefreshToken()
    {
        var raw      = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash     = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenDays);
        return (raw, hash, expiresAt);
    }
}
