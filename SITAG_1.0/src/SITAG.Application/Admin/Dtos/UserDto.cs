using SITAG.Domain.Enums;

namespace SITAG.Application.Admin.Dtos;

public sealed record UserDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    UserRole Role,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);
