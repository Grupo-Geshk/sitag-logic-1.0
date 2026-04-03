using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Common;
using SITAG.Domain.Entities;

namespace SITAG.Infrastructure.Persistence;

public sealed class SitagDbContext : DbContext, IApplicationDbContext
{
    public SitagDbContext(DbContextOptions<SitagDbContext> options) : base(options) { }

    // ── Core SaaS ────────────────────────────────────────────────────────────
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TenantAuditLog> TenantAuditLogs => Set<TenantAuditLog>();
    public DbSet<Producer> Producers => Set<Producer>();

    // ── Farms & Divisions ─────────────────────────────────────────────────────
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Division> Divisions => Set<Division>();

    // ── Animals & Trazabilidad ─────────────────────────────────────────────────
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<AnimalMovement> AnimalMovements => Set<AnimalMovement>();
    public DbSet<AnimalEvent> AnimalEvents => Set<AnimalEvent>();

    // ── Workers ───────────────────────────────────────────────────────────────
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<WorkerFarmAssignment> WorkerFarmAssignments => Set<WorkerFarmAssignment>();
    public DbSet<WorkerPayment> WorkerPayments => Set<WorkerPayment>();
    public DbSet<WorkerLoan> WorkerLoans => Set<WorkerLoan>();

    // ── Supplies / Inventory ──────────────────────────────────────────────────
    public DbSet<Supply> Supplies => Set<Supply>();
    public DbSet<SupplyMovement> SupplyMovements => Set<SupplyMovement>();
    public DbSet<SupplyLot> SupplyLots => Set<SupplyLot>();

    // ── Services ─────────────────────────────────────────────────────────────
    public DbSet<VetService> VetServices => Set<VetService>();
    public DbSet<ServiceAnimal> ServiceAnimals => Set<ServiceAnimal>();
    public DbSet<ServiceSupplyConsumption> ServiceSupplyConsumptions => Set<ServiceSupplyConsumption>();

    // ── Economy ───────────────────────────────────────────────────────────────
    public DbSet<TransactionCategory> TransactionCategories => Set<TransactionCategory>();
    public DbSet<EconomyTransaction> EconomyTransactions => Set<EconomyTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SitagDbContext).Assembly);
    }

    /// <summary>
    /// Automatically stamps CreatedAt on insert and UpdatedAt on update
    /// for all entities that inherit from BaseEntity.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
