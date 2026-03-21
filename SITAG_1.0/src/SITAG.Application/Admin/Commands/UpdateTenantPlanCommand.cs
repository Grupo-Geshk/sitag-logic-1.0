using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Enums;

namespace SITAG.Application.Admin.Commands;

public sealed record UpdateTenantPlanCommand(Guid TenantId, TenantPlan Plan) : IRequest;

public sealed class UpdateTenantPlanCommandHandler : IRequestHandler<UpdateTenantPlanCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateTenantPlanCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateTenantPlanCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == req.TenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant {req.TenantId} not found.");

        tenant.Plan = req.Plan;
        await _db.SaveChangesAsync(ct);
    }
}
