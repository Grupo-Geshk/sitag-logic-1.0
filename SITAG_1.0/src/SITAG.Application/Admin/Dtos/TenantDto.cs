using SITAG.Domain.Enums;

namespace SITAG.Application.Admin.Dtos;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string PrimaryEmail,
    TenantStatus Status,
    DateTimeOffset? PaidUntil,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record TenantDetailDto(
    Guid Id,
    string Name,
    string PrimaryEmail,
    TenantStatus Status,
    DateTimeOffset? PaidUntil,
    string? Notes,
    DateTimeOffset CreatedAt,
    int UserCount);

public sealed record TenantAuditEntryDto(
    Guid Id,
    Guid? ActorUserId,
    string ActorEmail,
    string Action,
    string? FromStatus,
    string? ToStatus,
    DateTimeOffset? PaidUntil,
    string? Note,
    DateTimeOffset CreatedAt);
