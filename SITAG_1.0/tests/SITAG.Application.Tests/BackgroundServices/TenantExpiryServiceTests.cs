using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SITAG.Application.Tests.Helpers;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;
using SITAG.Infrastructure.BackgroundServices;
using SITAG.Infrastructure.Persistence;

namespace SITAG.Application.Tests.BackgroundServices;

public sealed class TenantExpiryServiceTests
{
    private static IServiceScopeFactory CreateScopeFactory(SitagDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddScoped<SitagDbContext>(sp => sp.GetRequiredService<SitagDbContext>());
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task Run_ActiveTenantWithExpiredPaidUntil_DegradesToPastDue()
    {
        var db = DbContextFactory.Create();
        var tenant = new Tenant
        {
            Id           = Guid.NewGuid(),
            Name         = "Late Farmer",
            PrimaryEmail = "late@farm.com",
            Status       = TenantStatus.Active,
            PaidUntil    = DateTimeOffset.UtcNow.AddDays(-1), // expired yesterday
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var scopeFactory = CreateScopeFactory(db);
        var service = new TenantExpiryService(scopeFactory, NullLogger<TenantExpiryService>.Instance);

        // Invoke via reflection to call private RunAsync
        var method = typeof(TenantExpiryService)
            .GetMethod("RunAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)method.Invoke(service, [CancellationToken.None])!;

        var updated = db.Tenants.Find(tenant.Id)!;
        updated.Status.Should().Be(TenantStatus.PastDue);
        db.TenantAuditLogs.Should().HaveCount(1);
        db.TenantAuditLogs.First().ActorUserId.Should().BeNull();
        db.TenantAuditLogs.First().ToStatus.Should().Be(TenantStatus.PastDue);
    }

    [Fact]
    public async Task Run_ActiveTenantWithFuturePaidUntil_NotDegraded()
    {
        var db = DbContextFactory.Create();
        var tenant = new Tenant
        {
            Id           = Guid.NewGuid(),
            Name         = "Good Farmer",
            PrimaryEmail = "good@farm.com",
            Status       = TenantStatus.Active,
            PaidUntil    = DateTimeOffset.UtcNow.AddDays(30),
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var scopeFactory = CreateScopeFactory(db);
        var service = new TenantExpiryService(scopeFactory, NullLogger<TenantExpiryService>.Instance);

        var method = typeof(TenantExpiryService)
            .GetMethod("RunAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)method.Invoke(service, [CancellationToken.None])!;

        var updated = db.Tenants.Find(tenant.Id)!;
        updated.Status.Should().Be(TenantStatus.Active);
        db.TenantAuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task Run_PastDueTenantWithExpiredDate_NotChangedAgain()
    {
        var db = DbContextFactory.Create();
        var tenant = new Tenant
        {
            Id           = Guid.NewGuid(),
            Name         = "Already PastDue",
            PrimaryEmail = "past@farm.com",
            Status       = TenantStatus.PastDue,  // already degraded
            PaidUntil    = DateTimeOffset.UtcNow.AddDays(-5),
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var scopeFactory = CreateScopeFactory(db);
        var service = new TenantExpiryService(scopeFactory, NullLogger<TenantExpiryService>.Instance);

        var method = typeof(TenantExpiryService)
            .GetMethod("RunAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)method.Invoke(service, [CancellationToken.None])!;

        // Status unchanged, no new audit log (only Active -> PastDue, not PastDue -> PastDue)
        db.Tenants.Find(tenant.Id)!.Status.Should().Be(TenantStatus.PastDue);
        db.TenantAuditLogs.Should().BeEmpty();
    }
}
