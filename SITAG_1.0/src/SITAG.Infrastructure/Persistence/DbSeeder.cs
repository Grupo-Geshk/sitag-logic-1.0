using Microsoft.EntityFrameworkCore;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Infrastructure.Persistence;

/// <summary>
/// Seeds the two bootstrap accounts that must exist before the first login.
/// Idempotent — skips rows that already exist.
///
///   admin@sitag.app  / Admin123!   → AdminSistema  (system tenant)
///   demo@finca.com   / Demo123!    → Productor      (Demo Finca tenant)
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(SitagDbContext db)
    {
        await SeedAdminAsync(db);
        await SeedDemoProducerAsync(db);
        await db.SaveChangesAsync();
    }

    // ── Admin ─────────────────────────────────────────────────────────────────
    private static async Task SeedAdminAsync(SitagDbContext db)
    {
        const string email = "admin@sitag.app";
        if (await db.Users.AnyAsync(u => u.Email == email)) return;

        var tenant = new Tenant
        {
            Name         = "SITAG Sistema",
            PrimaryEmail = email,
            Status       = TenantStatus.Active,
            PaidUntil    = DateTimeOffset.UtcNow.AddYears(99),
        };

        var producer = new Producer
        {
            TenantId    = tenant.Id,
            DisplayName = "SITAG Sistema",
        };

        var user = new User
        {
            TenantId           = tenant.Id,
            Email              = email,
            PasswordHash       = BCrypt.Net.BCrypt.HashPassword("Admin123!", workFactor: 12),
            Role               = UserRole.AdminSistema,
            IsActive           = true,
            MustChangePassword = false,
            FirstName          = "Admin",
            LastName           = "Sistema",
        };

        db.Tenants.Add(tenant);
        db.Producers.Add(producer);
        db.Users.Add(user);
    }

    // ── Demo producer ─────────────────────────────────────────────────────────
    private static async Task SeedDemoProducerAsync(SitagDbContext db)
    {
        const string email = "demo@finca.com";
        if (await db.Users.AnyAsync(u => u.Email == email)) return;

        var tenant = new Tenant
        {
            Name         = "Demo Finca",
            PrimaryEmail = email,
            Status       = TenantStatus.Active,
            PaidUntil    = DateTimeOffset.UtcNow.AddYears(1),
        };

        var producer = new Producer
        {
            TenantId    = tenant.Id,
            DisplayName = "Demo Finca",
        };

        var user = new User
        {
            TenantId           = tenant.Id,
            Email              = email,
            PasswordHash       = BCrypt.Net.BCrypt.HashPassword("Demo123!", workFactor: 12),
            Role               = UserRole.Productor,
            IsActive           = true,
            MustChangePassword = false,
            FirstName          = "Demo",
            LastName           = "Productor",
        };

        db.Tenants.Add(tenant);
        db.Producers.Add(producer);
        db.Users.Add(user);
    }
}
