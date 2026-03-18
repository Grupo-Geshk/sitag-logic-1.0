using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SITAG.Infrastructure.Persistence;

namespace SITAG.Api.Tests.Infrastructure;

/// <summary>
/// WebApplicationFactory that replaces the PostgreSQL database with an
/// isolated in-memory EF Core database per test class.
/// </summary>
public sealed class SitagWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Suppress Serilog startup noise in test output
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");

        builder.ConfigureServices(services =>
        {
            // Remove the real PostgreSQL registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SitagDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add InMemory database
            services.AddDbContext<SitagDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Suppress background service during tests
            var hostedService = services.SingleOrDefault(
                d => d.ImplementationType?.Name == "TenantExpiryService");
            if (hostedService != null)
                services.Remove(hostedService);
        });

        builder.UseEnvironment("Development");

        // Inject required configuration that Program.cs reads from env/config
        builder.UseSetting("JWT:Issuer",    "test-issuer");
        builder.UseSetting("JWT:Audience",  "test-audience");
        builder.UseSetting("JWT:SigningKey", "super-secret-key-for-testing-1234567890!!");
        builder.UseSetting("JWT:AccessTokenMinutes", "15");
        builder.UseSetting("JWT:RefreshTokenDays",   "30");
        builder.UseSetting("CORS:AllowedOrigins", "http://localhost:3000");
        builder.UseSetting("ConnectionStrings:Default",
            "Host=localhost;Database=test;Username=test;Password=test");

        builder.ConfigureLogging(logging => logging.ClearProviders());
    }

    /// <summary>
    /// Seeds the in-memory database via the DI scope.
    /// Call this to populate test data before making requests.
    /// </summary>
    public void SeedDatabase(Action<SitagDbContext> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SitagDbContext>();
        seed(db);
    }
}
