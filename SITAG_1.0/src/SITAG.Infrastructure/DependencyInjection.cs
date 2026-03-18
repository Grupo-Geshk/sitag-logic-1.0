using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SITAG.Application.Common.Interfaces;
using SITAG.Infrastructure.BackgroundServices;
using SITAG.Infrastructure.Identity;
using SITAG.Infrastructure.Persistence;

namespace SITAG.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────────────────
        var rawConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings__Default environment variable is not set. " +
                "Set it via Railway environment config or a local .env file.");

        services.AddDbContext<SitagDbContext>(options =>
            options.UseNpgsql(
                ToNpgsqlConnectionString(rawConnectionString),
                npgsql => npgsql.MigrationsAssembly(typeof(SitagDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<SitagDbContext>());

        // ── JWT settings ──────────────────────────────────────────────────────
        services.Configure<JwtSettings>(configuration.GetSection("JWT"));

        // ── Identity / Auth services ──────────────────────────────────────────
        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // ── Background services ───────────────────────────────────────────────
        services.AddHostedService<TenantExpiryService>();

        return services;
    }

    /// <summary>
    /// Converts a PostgreSQL connection URI (postgresql://user:pass@host/db) to
    /// Npgsql key-value connection string. Returned unchanged if already key-value.
    /// </summary>
    private static string ToNpgsqlConnectionString(string value)
    {
        if (!value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            return value;

        var uri = new Uri(value);
        var parts = uri.UserInfo.Split(':');
        var username = Uri.UnescapeDataString(parts[0]);
        var password = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        return $"Host={uri.Host};Port={port};Database={database};" +
               $"Username={username};Password={password};" +
               $"SSL Mode=Prefer;Trust Server Certificate=true";
    }
}
