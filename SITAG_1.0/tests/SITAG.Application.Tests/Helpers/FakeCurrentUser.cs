using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Tests.Helpers;

/// <summary>
/// A simple test double for ICurrentUser / ICurrentTenant.
/// Tests can set TenantId/UserId/Role freely.
/// </summary>
public sealed class FakeCurrentUser : ICurrentUser, ICurrentTenant
{
    public bool IsAuthenticated { get; set; } = true;
    public Guid UserId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; } = Guid.NewGuid();
    public string Role { get; set; } = "AdminSistema";
}
