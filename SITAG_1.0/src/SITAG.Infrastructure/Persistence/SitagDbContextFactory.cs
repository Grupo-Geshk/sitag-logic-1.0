using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SITAG.Infrastructure.Persistence;

/// <summary>
/// Used only by EF Core tooling (dotnet ef migrations / database update).
/// Not used at runtime — the real DbContext is registered via DependencyInjection.cs.
/// </summary>
public sealed class SitagDbContextFactory : IDesignTimeDbContextFactory<SitagDbContext>
{
    public SitagDbContext CreateDbContext(string[] args)
    {
        // Load connection string from environment variable or appsettings
        var connectionString =
            Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Fallback: read from appsettings.json next to the Api project
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "SITAG.Api"))
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            connectionString = config.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "No connection string found. Set DATABASE_URL or ConnectionStrings__DefaultConnection env var.");

        var options = new DbContextOptionsBuilder<SitagDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SitagDbContext(options);
    }
}
