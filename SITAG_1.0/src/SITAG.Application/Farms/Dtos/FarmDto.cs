namespace SITAG.Application.Farms.Dtos;

public sealed record FarmBrandDto(
    Guid Id,
    string Name,
    string? PhotoUrl,
    DateTimeOffset CreatedAt);

public sealed record FarmDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Location,
    decimal? Hectares,
    string? FarmType,
    bool IsOwned,
    DateTimeOffset CreatedAt);

public sealed record FarmOverviewDto(
    Guid Id,
    string Name,
    string? Location,
    string? FarmType,
    bool IsOwned,
    int AnimalCount,
    int DivisionCount);

public sealed record FarmDetailDto(
    Guid Id,
    string Name,
    string? Location,
    decimal? Hectares,
    string? FarmType,
    bool IsOwned,
    DateTimeOffset CreatedAt);

public sealed record UpdateFarmRequest(
    string Name,
    string? Location,
    decimal? Hectares,
    string? FarmType,
    bool IsOwned);
