using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Supplies.Dtos;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Supplies.Commands;

internal static class SupplyMapper
{
    internal static SupplyDto ToDto(Supply s) => new(
        s.Id, s.TenantId, s.FarmId, s.Name, s.Category, s.Unit,
        s.CurrentQuantity, s.MinStockLevel, s.ExpirationDate,
        LowStock: s.CurrentQuantity <= s.MinStockLevel,
        s.CreatedAt);
}

// ── Create ────────────────────────────────────────────────────────────────────
public sealed record CreateSupplyCommand(
    Guid? FarmId, string Name, string? Category, string Unit,
    decimal InitialQuantity, decimal MinStockLevel, DateOnly? ExpirationDate) : IRequest<SupplyDto>;

public sealed class CreateSupplyHandler : IRequestHandler<CreateSupplyCommand, SupplyDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateSupplyHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<SupplyDto> Handle(CreateSupplyCommand r, CancellationToken ct)
    {
        var supply = new Supply
        {
            TenantId        = _user.TenantId,
            FarmId          = r.FarmId,
            Name            = r.Name.Trim(),
            Category        = r.Category?.Trim(),
            Unit            = r.Unit.Trim(),
            CurrentQuantity = r.InitialQuantity,
            MinStockLevel   = r.MinStockLevel,
            ExpirationDate  = r.ExpirationDate,
        };
        _db.Supplies.Add(supply);

        if (r.InitialQuantity > 0)
        {
            _db.SupplyMovements.Add(new SupplyMovement
            {
                TenantId         = _user.TenantId,
                SupplyId         = supply.Id,
                MovementType     = SupplyMovementType.Entry,
                Quantity         = r.InitialQuantity,
                PreviousQuantity = 0,
                NewQuantity      = r.InitialQuantity,
                Reason           = "Stock inicial",
                MovementDate     = DateTimeOffset.UtcNow,
                UserId           = _user.UserId,
            });
        }

        await _db.SaveChangesAsync(ct);
        return SupplyMapper.ToDto(supply);
    }
}

// ── Update ────────────────────────────────────────────────────────────────────
public sealed record UpdateSupplyCommand(
    Guid SupplyId, string Name, string? Category, string Unit,
    decimal MinStockLevel, DateOnly? ExpirationDate) : IRequest<SupplyDto>;

public sealed class UpdateSupplyHandler : IRequestHandler<UpdateSupplyCommand, SupplyDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UpdateSupplyHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<SupplyDto> Handle(UpdateSupplyCommand r, CancellationToken ct)
    {
        var s = await _db.Supplies
            .FirstOrDefaultAsync(s => s.Id == r.SupplyId && s.TenantId == _user.TenantId && s.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Supply {r.SupplyId} not found.");

        s.Name           = r.Name.Trim();
        s.Category       = r.Category?.Trim();
        s.Unit           = r.Unit.Trim();
        s.MinStockLevel  = r.MinStockLevel;
        s.ExpirationDate = r.ExpirationDate;
        await _db.SaveChangesAsync(ct);
        return SupplyMapper.ToDto(s);
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────
public sealed record DeleteSupplyCommand(Guid SupplyId) : IRequest;

public sealed class DeleteSupplyHandler : IRequestHandler<DeleteSupplyCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public DeleteSupplyHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(DeleteSupplyCommand r, CancellationToken ct)
    {
        var s = await _db.Supplies
            .FirstOrDefaultAsync(s => s.Id == r.SupplyId && s.TenantId == _user.TenantId && s.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Supply {r.SupplyId} not found.");
        s.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

// ── Adjust stock (Entry / Consumption / Adjustment) ───────────────────────────
public sealed record AdjustStockCommand(
    Guid SupplyId, SupplyMovementType MovementType,
    decimal Quantity, string? Reason) : IRequest<SupplyDto>;

public sealed class AdjustStockHandler : IRequestHandler<AdjustStockCommand, SupplyDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public AdjustStockHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<SupplyDto> Handle(AdjustStockCommand r, CancellationToken ct)
    {
        var s = await _db.Supplies
            .FirstOrDefaultAsync(s => s.Id == r.SupplyId && s.TenantId == _user.TenantId && s.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Supply {r.SupplyId} not found.");

        var prev = s.CurrentQuantity;
        var next = r.MovementType == SupplyMovementType.Consumption
            ? prev - r.Quantity
            : prev + r.Quantity;  // Entry or Adjustment

        if (next < 0) throw new InvalidOperationException("Insufficient stock.");

        s.CurrentQuantity = next;

        _db.SupplyMovements.Add(new SupplyMovement
        {
            TenantId         = _user.TenantId,
            SupplyId         = s.Id,
            MovementType     = r.MovementType,
            Quantity         = r.Quantity,
            PreviousQuantity = prev,
            NewQuantity      = next,
            Reason           = r.Reason?.Trim(),
            MovementDate     = DateTimeOffset.UtcNow,
            UserId           = _user.UserId,
        });

        await _db.SaveChangesAsync(ct);
        return SupplyMapper.ToDto(s);
    }
}

// ── Add supply consumption to a service (atomic: deduct stock + log movement) ─
public sealed record AddServiceConsumptionCommand(
    Guid ServiceId, Guid SupplyId, decimal Quantity,
    Guid? LotId = null) : IRequest<ServiceConsumptionDto>;

public sealed class AddServiceConsumptionHandler
    : IRequestHandler<AddServiceConsumptionCommand, ServiceConsumptionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public AddServiceConsumptionHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<ServiceConsumptionDto> Handle(AddServiceConsumptionCommand r, CancellationToken ct)
    {
        if (r.Quantity <= 0) throw new ArgumentException("Quantity must be positive.");
        var tid = _user.TenantId;

        var svc = await _db.VetServices
            .FirstOrDefaultAsync(s => s.Id == r.ServiceId && s.TenantId == tid, ct)
            ?? throw new KeyNotFoundException($"Service {r.ServiceId} not found.");

        var supply = await _db.Supplies
            .FirstOrDefaultAsync(s => s.Id == r.SupplyId && s.TenantId == tid && s.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Supply {r.SupplyId} not found.");

        if (supply.CurrentQuantity < r.Quantity)
            throw new InvalidOperationException(
                $"Insufficient stock for '{supply.Name}'. Available: {supply.CurrentQuantity} {supply.Unit}.");

        // ── Lot-level deduction ───────────────────────────────────────────────
        SupplyLot? lot = null;
        if (r.LotId.HasValue)
        {
            lot = await _db.SupplyLots
                .FirstOrDefaultAsync(l => l.Id == r.LotId.Value && l.SupplyId == supply.Id && l.TenantId == tid, ct)
                ?? throw new KeyNotFoundException($"Lot {r.LotId.Value} not found for supply {supply.Id}.");

            if (lot.Status == SupplyLotStatus.Agotado)
                throw new InvalidOperationException($"Lot is already depleted.");

            if (lot.CurrentQuantity < r.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient quantity in this lot. Available: {lot.CurrentQuantity} {supply.Unit}.");

            lot.CurrentQuantity -= r.Quantity;

            // Status transitions
            lot.Status = lot.CurrentQuantity <= 0
                ? SupplyLotStatus.Agotado
                : SupplyLotStatus.EnUso;
        }

        // ── Supply aggregate ──────────────────────────────────────────────────
        var prev = supply.CurrentQuantity;
        supply.CurrentQuantity -= r.Quantity;

        var consumption = new ServiceSupplyConsumption
        {
            TenantId  = tid,
            ServiceId = svc.Id,
            SupplyId  = supply.Id,
            Quantity  = r.Quantity,
        };
        _db.ServiceSupplyConsumptions.Add(consumption);

        _db.SupplyMovements.Add(new SupplyMovement
        {
            TenantId         = tid,
            SupplyId         = supply.Id,
            LotId            = lot?.Id,
            MovementType     = SupplyMovementType.Consumption,
            Quantity         = r.Quantity,
            PreviousQuantity = prev,
            NewQuantity      = supply.CurrentQuantity,
            Reason           = $"Consumo en servicio {svc.Id}",
            MovementDate     = DateTimeOffset.UtcNow,
            UserId           = _user.UserId,
        });

        await _db.SaveChangesAsync(ct);

        return new ServiceConsumptionDto(
            consumption.Id, consumption.ServiceId, consumption.SupplyId,
            supply.Name, supply.Unit, consumption.Quantity, consumption.CreatedAt);
    }
}
