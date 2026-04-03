using SITAG.Domain.Enums;

namespace SITAG.Application.Supplies.Dtos;

public sealed record SupplyDto(
    Guid Id, Guid TenantId, Guid? FarmId,
    string Name, string? Category, string Unit,
    decimal CurrentQuantity, decimal MinStockLevel,
    DateOnly? ExpirationDate,
    bool LowStock,
    DateTimeOffset CreatedAt);

public sealed record SupplyMovementDto(
    Guid Id, Guid SupplyId, string MovementType,
    decimal Quantity, decimal PreviousQuantity, decimal NewQuantity,
    string? Reason, DateTimeOffset MovementDate, DateTimeOffset CreatedAt);

public sealed record SupplyAlertDto(
    Guid SupplyId,
    string SupplyName,
    string AlertType,        // "LOW_STOCK" | "EXPIRING"
    string Severity,         // "Alta" | "Media" | "Baja"
    string Message,
    decimal? CurrentQuantity,
    decimal? MinStockLevel,
    DateOnly? ExpirationDate);

public sealed record SupplyUsageDto(
    Guid SupplyId,
    string SupplyName,
    string Unit,
    decimal TotalConsumed,
    int MovementCount);

public sealed record SupplyLotDto(
    Guid Id,
    Guid SupplyId,
    decimal InitialQuantity,
    decimal CurrentQuantity,
    decimal? UnitCost,
    string? Supplier,
    DateOnly? ExpirationDate,
    DateOnly PurchaseDate,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record ServiceConsumptionDto(
    Guid Id,
    Guid ServiceId,
    Guid SupplyId,
    string SupplyName,
    string SupplyUnit,
    decimal Quantity,
    DateTimeOffset CreatedAt);
