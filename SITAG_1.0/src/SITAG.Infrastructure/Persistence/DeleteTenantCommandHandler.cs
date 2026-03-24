using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Commands;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Infrastructure.Persistence;

/// <summary>
/// Handler for <see cref="DeleteTenantCommand"/>.
/// Lives in Infrastructure because it uses EF Core's ExecuteDeleteAsync (Relational).
///
/// Deletion order respects FK dependencies (children before parents).
/// </summary>
public sealed class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteTenantCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteTenantCommand req, CancellationToken ct)
    {
        var tid = req.TenantId;

        var exists = await _db.Tenants.AnyAsync(t => t.Id == tid, ct);
        if (!exists)
            throw new KeyNotFoundException($"Tenant {tid} not found.");

        // ── 1. Leaf / junction tables ─────────────────────────────────────────
        await _db.WorkerPayments              .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.WorkerFarmAssignments       .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.ServiceSupplyConsumptions   .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.ServiceAnimals              .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.SupplyMovements             .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.AnimalMovements             .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.AnimalEvents                .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.TenantAuditLogs             .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.RefreshTokens               .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);

        // ── 2. Aggregate roots ────────────────────────────────────────────────
        await _db.VetServices                 .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.Supplies                    .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.EconomyTransactions         .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.TransactionCategories       .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.Animals                     .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.Divisions                   .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.Farms                       .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.Workers                     .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.Users                       .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);
        await _db.Producers                   .Where(x => x.TenantId == tid).ExecuteDeleteAsync(ct);

        // ── 3. Tenant itself ──────────────────────────────────────────────────
        await _db.Tenants                     .Where(x => x.Id == tid).ExecuteDeleteAsync(ct);
    }
}
