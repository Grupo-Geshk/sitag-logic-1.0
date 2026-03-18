using SITAG.Domain.Enums;

namespace SITAG.Application.Workers.Dtos;

public sealed record WorkerPaymentDto(
    Guid Id,
    Guid WorkerId,
    PaymentMode Mode,
    DateOnly PaymentDate,
    decimal Rate,
    decimal Quantity,
    decimal TotalAmount,
    string? Notes,
    Guid? FarmId,
    Guid? TransactionId,
    DateTimeOffset CreatedAt);

public sealed record WorkerActivityEntryDto(
    string EntryType,       // "EVENT" | "SERVICE"
    DateTimeOffset Date,
    string Description,
    Guid? AnimalId,
    Guid? ServiceId,
    decimal? Cost);

public sealed record WorkerActivityTimelineDto(
    Guid WorkerId,
    IReadOnlyList<WorkerActivityEntryDto> Entries);

public sealed record WorkerDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? RoleLabel,
    string? Contact,
    WorkerStatus Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<Guid> AssignedFarmIds);

public sealed record WorkerAssignmentDto(
    Guid Id,
    Guid WorkerId,
    Guid FarmId,
    DateOnly StartDate,
    DateOnly? EndDate);
