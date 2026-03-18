using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Economy.Dtos;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Economy.Commands;

// ── Toggle category active ────────────────────────────────────────────────────
public sealed record SetCategoryActiveCommand(Guid CategoryId, bool IsActive) : IRequest;

public sealed class SetCategoryActiveHandler : IRequestHandler<SetCategoryActiveCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public SetCategoryActiveHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(SetCategoryActiveCommand r, CancellationToken ct)
    {
        var cat = await _db.TransactionCategories
            .FirstOrDefaultAsync(c => c.Id == r.CategoryId && c.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Category {r.CategoryId} not found.");
        cat.IsActive = r.IsActive;
        await _db.SaveChangesAsync(ct);
    }
}

// ── Create category ───────────────────────────────────────────────────────────
public sealed record CreateCategoryCommand(string Name, TransactionType Type) : IRequest<TransactionCategoryDto>;

public sealed class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, TransactionCategoryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateCategoryHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<TransactionCategoryDto> Handle(CreateCategoryCommand r, CancellationToken ct)
    {
        var cat = new TransactionCategory
        {
            TenantId = _user.TenantId,
            Name     = r.Name.Trim(),
            Type     = r.Type,
        };
        _db.TransactionCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        return new TransactionCategoryDto(cat.Id, cat.Name, cat.Type, cat.IsActive);
    }
}

// ── Create transaction ────────────────────────────────────────────────────────
public sealed record CreateTransactionCommand(
    TransactionType Type, Guid? CategoryId, string? CategoryName,
    string? Description, decimal Amount,
    DateTimeOffset TxnDate, Guid? FarmId) : IRequest<TransactionDto>;

public sealed class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateTransactionHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<TransactionDto> Handle(CreateTransactionCommand r, CancellationToken ct)
    {
        if (r.Amount <= 0) throw new ArgumentException("Amount must be positive.");

        string? catName = r.CategoryName;
        if (r.CategoryId.HasValue && catName is null)
        {
            catName = await _db.TransactionCategories
                .Where(c => c.Id == r.CategoryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }

        var txn = new EconomyTransaction
        {
            TenantId        = _user.TenantId,
            Type            = r.Type,
            CategoryId      = r.CategoryId,
            CategoryName    = catName,
            Description     = r.Description?.Trim(),
            Amount          = r.Amount,
            TxnDate         = r.TxnDate,
            FarmId          = r.FarmId,
            CreatedByUserId = _user.UserId,
        };
        _db.EconomyTransactions.Add(txn);
        await _db.SaveChangesAsync(ct);
        return ToDto(txn);
    }

    internal static TransactionDto ToDto(EconomyTransaction t) => new(
        t.Id, t.TenantId, t.Type, t.CategoryId, t.CategoryName,
        t.Description, t.Amount, t.TxnDate, t.FarmId, t.CreatedAt);
}

// ── Soft delete transaction ───────────────────────────────────────────────────
public sealed record DeleteTransactionCommand(Guid TransactionId) : IRequest;

public sealed class DeleteTransactionHandler : IRequestHandler<DeleteTransactionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public DeleteTransactionHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(DeleteTransactionCommand r, CancellationToken ct)
    {
        var txn = await _db.EconomyTransactions
            .FirstOrDefaultAsync(t => t.Id == r.TransactionId && t.TenantId == _user.TenantId && t.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Transaction {r.TransactionId} not found.");

        txn.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
