using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SITAG.Api.Tests.Infrastructure;

/// <summary>
/// Generates JWT tokens for integration tests using the same key configured
/// in SitagWebApplicationFactory.
/// </summary>
public static class JwtTokenHelper
{
    private const string Issuer    = "test-issuer";
    private const string Audience  = "test-audience";
    private const string SigningKey = "super-secret-key-for-testing-1234567890!!";

    public static string GenerateToken(
        Guid userId,
        Guid tenantId,
        string role  = "AdminSistema",
        string email = "user@test.com",
        int expiresInMinutes = 15)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tenantId", tenantId.ToString()),
            new Claim(ClaimTypes.Role, role),
        };

        var token = new JwtSecurityToken(
            issuer:             Issuer,
            audience:           Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string ExpiredToken(Guid userId, Guid tenantId, string role = "AdminSistema")
        => GenerateToken(userId, tenantId, role, expiresInMinutes: -1);
}
