namespace SITAG.Application.Dashboard.Dtos;

public sealed record DashboardDto(
    int TotalAnimales,
    int AnimalesActivos,
    int AnimalesEnfermos,
    int Natalidad30Dias,
    int Mortalidad30Dias,
    decimal IngresosMes,
    decimal EgresosMes,
    decimal Margen,
    IReadOnlyList<FarmDistributionDto> DistribucionPorFinca);

public sealed record FarmDistributionDto(
    Guid FarmId,
    string FarmName,
    int TotalAnimales);

public sealed record DashboardAlertDto(
    string Type,       // "ANIMAL_SICK" | "LOW_STOCK" | "EXPIRING_SUPPLY"
    string Severity,   // "Alta" | "Media" | "Baja"
    string Message,
    Guid? EntityId,
    string? EntityName);
