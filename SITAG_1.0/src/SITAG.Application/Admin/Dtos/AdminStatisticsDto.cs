namespace SITAG.Application.Admin.Dtos;

public sealed record AdminStatisticsDto(
    int TotalTenants,
    int ActiveTenants,
    int PastDueTenants,
    int DelinquentTenants,
    int TotalUsers,
    int TotalProducers,
    int TotalAnimals,
    int TotalFarms);
