namespace SITAG.Application.Common.Interfaces;

/// <summary>
/// Focused interface for handlers that only need tenant isolation context.
/// Resolved from ICurrentUser; never accepted from the request.
/// </summary>
public interface ICurrentTenant
{
    Guid TenantId { get; }
}
