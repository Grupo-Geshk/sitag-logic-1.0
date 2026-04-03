using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Supplies.Dtos;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Supplies.Commands;

internal static class SupplyLotMapper
{
    internal static SupplyLotDto ToDto(SupplyLot l) => new(
        l.Id, l.SupplyId, l.InitialQuantity, l.CurrentQuantity,
        l.UnitCost, l.Supplier, l.ExpirationDate, l.PurchaseDate,
        l.Status.ToString(), l.Notes, l.CreatedAt);
}

// ── Create lot ────────────────────────────────────────────────────────────────
public sealed record CreateSupplyLotCommand(
    Guid SupplyId,
    decimal Quantity,
    decimal? UnitCost,
    string? Supplier,
    DateOnly? ExpirationDate,
    DateOnly PurchaseDate,
    string? Notes) : IRequest<SupplyLotDto>;

public sealed class CreateSupplyLotHandler : IRequestHandler<CreateSupplyLotCommand, SupplyLotDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateSupplyLotHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<SupplyLotDto> Handle(CreateSupplyLotCommand r, CancellationToken ct)
    {
        var supply = await _db.Supplies
            .FirstOrDefaultAsync(s => s.Id == r.SupplyId && s.TenantId == _user.TenantId && s.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Supply {r.SupplyId} not found.");

        var lot = new SupplyLot
        {
            TenantId         = _user.TenantId,
            SupplyId         = supply.Id,
            InitialQuantity  = r.Quantity,
            CurrentQuantity  = r.Quantity,
            UnitCost         = r.UnitCost,
            Supplier         = r.Supplier?.Trim(),
            ExpirationDate   = r.ExpirationDate,
            PurchaseDate     = r.PurchaseDate,
            Notes            = r.Notes?.Trim(),
            Status           = SupplyLotStatus.EnStock,
        };
        _db.SupplyLots.Add(lot);

        // Update supply aggregate quantity
        var prev = supply.CurrentQuantity;
        supply.CurrentQuantity += r.Quantity;

        _db.SupplyMovements.Add(new SupplyMovement
        {
            TenantId         = _user.TenantId,
            SupplyId         = supply.Id,
            LotId            = lot.Id,
            MovementType     = SupplyMovementType.Entry,
            Quantity         = r.Quantity,
            PreviousQuantity = prev,
            NewQuantity      = supply.CurrentQuantity,
            Reason           = r.Supplier != null ? $"Compra — {r.Supplier}" : "Ingreso de lote",
            MovementDate     = DateTimeOffset.UtcNow,
            UserId           = _user.UserId,
        });

        await _db.SaveChangesAsync(ct);
        return SupplyLotMapper.ToDto(lot);
    }
}

// ── Update lot ────────────────────────────────────────────────────────────────
public sealed record UpdateSupplyLotCommand(
    Guid LotId,
    decimal? UnitCost,
    string? Supplier,
    DateOnly? ExpirationDate,
    DateOnly PurchaseDate,
    SupplyLotStatus Status,
    string? Notes) : IRequest<SupplyLotDto>;

public sealed class UpdateSupplyLotHandler : IRequestHandler<UpdateSupplyLotCommand, SupplyLotDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UpdateSupplyLotHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<SupplyLotDto> Handle(UpdateSupplyLotCommand r, CancellationToken ct)
    {
        var lot = await _db.SupplyLots
            .FirstOrDefaultAsync(l => l.Id == r.LotId && l.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Lot {r.LotId} not found.");

        lot.UnitCost       = r.UnitCost;
        lot.Supplier       = r.Supplier?.Trim();
        lot.ExpirationDate = r.ExpirationDate;
        lot.PurchaseDate   = r.PurchaseDate;
        lot.Status         = r.Status;
        lot.Notes          = r.Notes?.Trim();

        await _db.SaveChangesAsync(ct);
        return SupplyLotMapper.ToDto(lot);
    }
}
