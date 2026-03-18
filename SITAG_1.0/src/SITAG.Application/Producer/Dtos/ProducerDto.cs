namespace SITAG.Application.Producers.Dtos;

public sealed record ProducerDto(Guid Id, Guid TenantId, string DisplayName, DateTimeOffset CreatedAt);
