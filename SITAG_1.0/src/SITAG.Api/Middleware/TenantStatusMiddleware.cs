using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SITAG.Domain.Enums;
using SITAG.Infrastructure.Persistence;

namespace SITAG.Api.Middleware;

/// <summary>
/// Enforces tenant billing state (REQ-TENANT-04).
///
/// Rules:
///   ACTIVE      → pass through (no header added)
///   PAST_DUE    → pass through + X-Tenant-Status: PAST_DUE header
///   DELINQUENT  → HTTP 402 { code: "TENANT_DELINQUENT" }
///
/// Skipped for:
///   • /auth/**    (login, refresh, logout — unauthenticated routes)
///   • /admin/**   (system admin is never blocked)
///   • /health, /version, /swagger/** (infra/ops routes)
///   • Unauthenticated requests (no JWT claims present)
/// </summary>
public sealed class TenantStatusMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    // Prefixes that are always allowed regardless of tenant status
    private static readonly string[] BypassPrefixes =
        ["/auth", "/admin", "/health", "/version", "/swagger", "/favicon"];

    public TenantStatusMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip bypass paths
        if (BypassPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Skip unauthenticated requests — let authorization middleware handle 401
        var tenantIdClaim = context.User.FindFirstValue("tenantId");
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            await _next(context);
            return;
        }

        // Query tenant status in a dedicated scope (middleware is Singleton)
        TenantStatus status;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SitagDbContext>();
            var tenant = await db.Tenants
                .AsNoTracking()
                .Select(t => new { t.Id, t.Status })
                .FirstOrDefaultAsync(t => t.Id == tenantId, context.RequestAborted);

            if (tenant is null)
            {
                // Tenant referenced in JWT no longer exists → treat as delinquent
                await WriteDelinquentAsync(context);
                return;
            }

            status = tenant.Status;
        }

        switch (status)
        {
            case TenantStatus.Delinquent:
                await WriteDelinquentAsync(context);
                return;

            case TenantStatus.PastDue:
                context.Response.Headers["X-Tenant-Status"] = "PAST_DUE";
                break;

            // Active → no action needed
        }

        await _next(context);
    }

    private static async Task WriteDelinquentAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            code    = "TENANT_DELINQUENT",
            message = "Access is suspended due to an outstanding balance. Please contact support."
        }));
    }
}
