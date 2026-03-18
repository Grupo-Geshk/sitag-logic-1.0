using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;
using SITAG.Infrastructure.Persistence;

namespace SITAG.Infrastructure.BackgroundServices;

/// <summary>
/// Daily background job: degrades Active tenants to PastDue when PaidUntil has passed (REQ-TENANT-07).
/// </summary>
public sealed class TenantExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TenantExpiryService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public TenantExpiryService(IServiceScopeFactory scopeFactory, ILogger<TenantExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once immediately on startup, then every 24 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db  = scope.ServiceProvider.GetRequiredService<SitagDbContext>();
            var now = DateTimeOffset.UtcNow;

            var expired = await db.Tenants
                .Where(t => t.Status == TenantStatus.Active
                         && t.PaidUntil.HasValue
                         && t.PaidUntil.Value < now)
                .ToListAsync(ct);

            if (expired.Count == 0) return;

            foreach (var tenant in expired)
            {
                var log = new TenantAuditLog
                {
                    TenantId    = tenant.Id,
                    ActorUserId = null,               // system actor
                    ActorEmail  = "system@sitag.app",
                    Action      = TenantAuditAction.StatusChange,
                    FromStatus  = tenant.Status,
                    ToStatus    = TenantStatus.PastDue,
                    PaidUntil   = tenant.PaidUntil,
                    Note        = $"Auto-degraded: PaidUntil={tenant.PaidUntil:O}",
                };
                tenant.Status = TenantStatus.PastDue;
                db.TenantAuditLogs.Add(log);
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "TenantExpiryService: degraded {Count} tenant(s) to PastDue.", expired.Count);
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TenantExpiryService: unexpected error.");
        }
    }
}
