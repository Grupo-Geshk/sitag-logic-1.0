using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Infrastructure.Identity;

/// <summary>
/// Reads the current user's identity from JWT claims via IHttpContextAccessor.
/// Registered as Scoped — one instance per HTTP request.
/// </summary>
public sealed class CurrentUser : ICurrentUser, ICurrentTenant
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId => ParseClaim(JwtRegisteredClaimNames.Sub)
        ?? throw new UnauthorizedAccessException("User is not authenticated.");

    public Guid TenantId => ParseClaim("tenantId")
        ?? throw new UnauthorizedAccessException("Tenant claim is missing from token.");

    public string Role =>
        Principal?.FindFirstValue("role") ?? string.Empty;

    private Guid? ParseClaim(string claimType)
    {
        var value = Principal?.FindFirstValue(claimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}

// Constant for claim name lookup (mirrors TokenService)
file static class JwtRegisteredClaimNames
{
    internal const string Sub = "sub";
}
