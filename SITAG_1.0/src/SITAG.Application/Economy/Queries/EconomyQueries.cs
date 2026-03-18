using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Economy.Commands;
using SITAG.Application.Economy.Dtos;
using SITAG.Domain.Enums;

namespace SITAG.Application.Economy.Queries;

// ── Categories ────────────────────────────────────────────────────────────────
public sealed record GetCategoriesQuery(TransactionType? Type) : IRequest<IReadOnlyList<TransactionCategoryDto>>;

public sealed class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<TransactionCategoryDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetCategoriesHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<TransactionCategoryDto>> Handle(GetCategoriesQuery r, CancellationToken ct)
    {
        var query = _db.TransactionCategories.AsNoTracking()
            .Where(c => c.TenantId == _user.TenantId && c.IsActive);
        if (r.Type.HasValue) query = query.Where(c => c.Type == r.Type);
        return await query.OrderBy(c => c.Name)
            .Select(c => new TransactionCategoryDto(c.Id, c.Name, c.Type, c.IsActive))
            .ToListAsync(ct);
    }
}

// ── Transactions list ─────────────────────────────────────────────────────────
public sealed record GetTransactionsQuery(
    TransactionType? Type, Guid? FarmId, Guid? CategoryId,
    DateTimeOffset? From, DateTimeOffset? To,
    int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<TransactionDto>>;

public sealed class GetTransactionsHandler : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetTransactionsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionsQuery r, CancellationToken ct)
    {
        var query = _db.EconomyTransactions.AsNoTracking()
            .Where(t => t.TenantId == _user.TenantId && t.DeletedAt == null);

        if (r.Type.HasValue)       query = query.Where(t => t.Type == r.Type);
        if (r.FarmId.HasValue)     query = query.Where(t => t.FarmId == r.FarmId);
        if (r.CategoryId.HasValue) query = query.Where(t => t.CategoryId == r.CategoryId);
        if (r.From.HasValue)       query = query.Where(t => t.TxnDate >= r.From);
        if (r.To.HasValue)         query = query.Where(t => t.TxnDate <= r.To);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.TxnDate)
            .Skip((r.PageNumber - 1) * r.PageSize)
            .Take(r.PageSize)
            .Select(t => new TransactionDto(t.Id, t.TenantId, t.Type, t.CategoryId, t.CategoryName,
                t.Description, t.Amount, t.TxnDate, t.FarmId, t.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<TransactionDto>(items, total, r.PageNumber, r.PageSize);
    }
}

// ── Summary (KPIs) ────────────────────────────────────────────────────────────
public sealed record GetEconomySummaryQuery(
    DateTimeOffset From, DateTimeOffset To, Guid? FarmId) : IRequest<EconomySummaryDto>;

public sealed class GetEconomySummaryHandler : IRequestHandler<GetEconomySummaryQuery, EconomySummaryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetEconomySummaryHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<EconomySummaryDto> Handle(GetEconomySummaryQuery r, CancellationToken ct)
    {
        var query = _db.EconomyTransactions.AsNoTracking()
            .Where(t => t.TenantId == _user.TenantId && t.DeletedAt == null
                     && t.TxnDate >= r.From && t.TxnDate <= r.To);
        if (r.FarmId.HasValue) query = query.Where(t => t.FarmId == r.FarmId);

        var txns = await query
            .Select(t => new
            {
                t.Type, t.Amount,
                t.CategoryId, t.CategoryName,
                t.FarmId
            })
            .ToListAsync(ct);

        var income  = txns.Where(t => t.Type == TransactionType.Ingreso).Sum(t => t.Amount);
        var expense = txns.Where(t => t.Type == TransactionType.Egreso).Sum(t => t.Amount);

        // Per-category breakdown
        var byCategory = txns
            .GroupBy(t => new { t.CategoryId, Name = t.CategoryName ?? "Sin categoría" })
            .Select(g => new CategoryBreakdownDto(
                g.Key.CategoryId,
                g.Key.Name,
                g.Where(t => t.Type == TransactionType.Ingreso).Sum(t => t.Amount),
                g.Where(t => t.Type == TransactionType.Egreso).Sum(t => t.Amount),
                g.Where(t => t.Type == TransactionType.Ingreso).Sum(t => t.Amount)
                    - g.Where(t => t.Type == TransactionType.Egreso).Sum(t => t.Amount)))
            .OrderByDescending(c => c.Income + c.Expense)
            .ToList();

        // Per-farm breakdown (load farm names separately)
        var farmIds = txns.Where(t => t.FarmId.HasValue).Select(t => t.FarmId!.Value).Distinct().ToList();
        var farmNames = await _db.Farms.AsNoTracking()
            .Where(f => farmIds.Contains(f.Id))
            .Select(f => new { f.Id, f.Name })
            .ToDictionaryAsync(f => f.Id, f => f.Name, ct);

        var byFarm = txns
            .GroupBy(t => t.FarmId)
            .Select(g =>
            {
                var name = g.Key.HasValue && farmNames.TryGetValue(g.Key.Value, out var n) ? n : "Sin finca";
                return new FarmBreakdownDto(
                    g.Key,
                    name,
                    g.Where(t => t.Type == TransactionType.Ingreso).Sum(t => t.Amount),
                    g.Where(t => t.Type == TransactionType.Egreso).Sum(t => t.Amount),
                    g.Where(t => t.Type == TransactionType.Ingreso).Sum(t => t.Amount)
                        - g.Where(t => t.Type == TransactionType.Egreso).Sum(t => t.Amount));
            })
            .OrderByDescending(f => f.Income + f.Expense)
            .ToList();

        return new EconomySummaryDto(income, expense, income - expense, r.From, r.To, byCategory, byFarm);
    }
}

// ── Economic trends by period (REQ-ECON-03) ───────────────────────────────────
public sealed record GetEconomyTrendsQuery(
    string          Period,       // "monthly" | "weekly"
    DateTimeOffset  StartDate,
    DateTimeOffset  EndDate,
    Guid?           FarmId) : IRequest<IReadOnlyList<EconomyTrendPointDto>>;

public sealed class GetEconomyTrendsHandler
    : IRequestHandler<GetEconomyTrendsQuery, IReadOnlyList<EconomyTrendPointDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetEconomyTrendsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<IReadOnlyList<EconomyTrendPointDto>> Handle(GetEconomyTrendsQuery r, CancellationToken ct)
    {
        var query = _db.EconomyTransactions.AsNoTracking()
            .Where(t => t.TenantId == _user.TenantId && t.DeletedAt == null
                     && t.TxnDate >= r.StartDate && t.TxnDate <= r.EndDate);
        if (r.FarmId.HasValue) query = query.Where(t => t.FarmId == r.FarmId);

        var txns = await query
            .Select(t => new { t.Type, t.Amount, t.TxnDate })
            .ToListAsync(ct);

        bool monthly = r.Period.Equals("monthly", StringComparison.OrdinalIgnoreCase);

        var grouped = txns
            .GroupBy(t => monthly
                ? new DateTimeOffset(t.TxnDate.Year, t.TxnDate.Month, 1, 0, 0, 0, TimeSpan.Zero)
                : StartOfWeek(t.TxnDate))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var income  = g.Where(t => t.Type == TransactionType.Ingreso).Sum(t => t.Amount);
                var expense = g.Where(t => t.Type == TransactionType.Egreso).Sum(t => t.Amount);
                var label   = monthly
                    ? g.Key.ToString("yyyy-MM")
                    : $"{g.Key:yyyy}-W{System.Globalization.ISOWeek.GetWeekOfYear(g.Key.DateTime):D2}";
                return new EconomyTrendPointDto(label, g.Key, income, expense, income - expense);
            })
            .ToList();

        return grouped;
    }

    private static DateTimeOffset StartOfWeek(DateTimeOffset dt)
    {
        var diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
        return new DateTimeOffset(dt.Date.AddDays(-diff), TimeSpan.Zero);
    }
}
