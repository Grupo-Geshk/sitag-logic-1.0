using MediatR;

namespace SITAG.Application.Admin.Commands;

/// <summary>
/// Hard-deletes ALL data belonging to a tenant: animals, farms, users, events,
/// transactions, services, supplies, workers and the tenant record itself.
///
/// This is irreversible. Intended for removing duplicate/test tenants and for
/// future automated inactivity cleanup.
///
/// Handler lives in SITAG.Infrastructure (uses EF Core bulk-delete / ExecuteDeleteAsync).
/// </summary>
public sealed record DeleteTenantCommand(Guid TenantId) : IRequest;
