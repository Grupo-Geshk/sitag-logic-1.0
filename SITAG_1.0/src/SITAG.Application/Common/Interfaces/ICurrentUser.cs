namespace SITAG.Application.Common.Interfaces;

/// <summary>
/// Provides the identity of the currently authenticated user, extracted from
/// JWT claims. Handlers must never accept tenantId or userId from request payloads;
/// they must always read from this service (DATABASE_MODEL.md §2.1).
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    Guid TenantId { get; }
    string Role { get; }
}
