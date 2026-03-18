using Microsoft.EntityFrameworkCore;
using SITAG.Infrastructure.Persistence;

namespace SITAG.Application.Tests.Helpers;

/// <summary>
/// Creates an isolated, in-memory SitagDbContext per test.
/// Uses a unique DB name so tests don't share state.
/// </summary>
public static class DbContextFactory
{
    public static SitagDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<SitagDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new SitagDbContext(options);
    }
}
