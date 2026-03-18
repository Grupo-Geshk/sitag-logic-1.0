using Microsoft.EntityFrameworkCore;
using SITAG.Domain.Entities;

namespace SITAG.Application.Common.Interfaces;

/// <summary>
/// Application-layer contract over the database context.
/// DbSets are added here incrementally as features are implemented.
/// Handlers depend on this interface; the EF Core implementation lives in Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    // ── Core SaaS ────────────────────────────────────────────────────────────
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<TenantAuditLog> TenantAuditLogs { get; }
    DbSet<Producer> Producers { get; }

    // ── Farms & Divisions ────────────────────────────────────────────────────
    DbSet<Farm> Farms { get; }
    DbSet<Division> Divisions { get; }

    // ── Animals & Traceability ───────────────────────────────────────────────
    DbSet<Animal> Animals { get; }
    DbSet<AnimalMovement> AnimalMovements { get; }
    DbSet<AnimalEvent> AnimalEvents { get; }

    // ── Workers ──────────────────────────────────────────────────────────────
    DbSet<Worker> Workers { get; }
    DbSet<WorkerFarmAssignment> WorkerFarmAssignments { get; }
    DbSet<WorkerPayment> WorkerPayments { get; }

    // ── Supplies & Inventory ─────────────────────────────────────────────────
    DbSet<Supply> Supplies { get; }
    DbSet<SupplyMovement> SupplyMovements { get; }

    // ── Vet Services ─────────────────────────────────────────────────────────
    DbSet<VetService> VetServices { get; }
    DbSet<ServiceAnimal> ServiceAnimals { get; }
    DbSet<ServiceSupplyConsumption> ServiceSupplyConsumptions { get; }

    // ── Economy ──────────────────────────────────────────────────────────────
    DbSet<TransactionCategory> TransactionCategories { get; }
    DbSet<EconomyTransaction> EconomyTransactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
