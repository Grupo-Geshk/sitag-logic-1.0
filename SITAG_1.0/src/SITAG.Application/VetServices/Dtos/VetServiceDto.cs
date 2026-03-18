using SITAG.Domain.Enums;

namespace SITAG.Application.VetServices.Dtos;

public sealed record VetServiceDto(
    Guid Id,
    Guid TenantId,
    Guid FarmId,
    Guid? DivisionId,
    Guid? WorkerId,
    string ServiceType,
    ServiceStatus Status,
    DateTimeOffset ScheduledDate,
    DateTimeOffset? CompletedDate,
    decimal? Cost,
    string? Notes,
    IReadOnlyList<Guid> AnimalIds,
    DateTimeOffset CreatedAt);
