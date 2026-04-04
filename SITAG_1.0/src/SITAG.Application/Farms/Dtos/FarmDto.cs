namespace SITAG.Application.Farms.Dtos;

public sealed record FarmBrandDto(
    Guid Id,
    Guid FarmId,
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

public sealed record FarmDetailDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Location,
    decimal? Hectares,
    string? FarmType,
    bool IsOwned,
    DateTimeOffset CreatedAt,
    int ActiveAnimals,
    int SickAnimals,
    int DivisionCount,
    int ActiveWorkers);

public sealed record FarmsOverviewDto(
    IReadOnlyList<FarmDetailDto> Farms,
    int TotalFarms,
    int TotalActiveAnimals,
    int TotalSickAnimals);

public sealed record DivisionDto(
    Guid Id,
    Guid FarmId,
    string Name,
    int? MaxCapacity,
    bool IsActive,
    DateTimeOffset CreatedAt,
    int AnimalCount);
