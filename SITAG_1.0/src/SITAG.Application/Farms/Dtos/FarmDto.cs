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
    int WorkerCount);

public sealed record UpdateFarmRequest(
    string Name,
    string? Location,
    decimal? Hectares,
    string? FarmType,
    bool IsOwned);

public sealed record DivisionDto(
    Guid Id,
    Guid FarmId,
    string Name,
    int? MaxCapacity,
    bool IsActive,
    DateTimeOffset CreatedAt,
    int AnimalCount);

public sealed record FarmsOverviewDto(
    IReadOnlyList<FarmDetailDto> Farms,
    int TotalFarms,
    int TotalActiveAnimals,
    int TotalSickAnimals);
