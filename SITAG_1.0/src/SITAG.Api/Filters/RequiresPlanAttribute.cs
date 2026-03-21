using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SITAG.Application.Common.Plans;
using SITAG.Domain.Enums;

namespace SITAG.Api.Filters;

/// <summary>
/// Blocks access to an action/controller if the caller's tenant plan
/// is below the required minimum plan. Returns 403 with a clear message.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresPlanAttribute : Attribute, IAuthorizationFilter
{
    private readonly TenantPlan _minPlan;

    public RequiresPlanAttribute(TenantPlan minPlan) => _minPlan = minPlan;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var planClaim = context.HttpContext.User.FindFirst("plan")?.Value;

        if (!Enum.TryParse<TenantPlan>(planClaim, out var callerPlan))
        {
            // No plan claim — treat as lowest tier
            callerPlan = TenantPlan.Semilla;
        }

        if (!PlanLimits.CanAccess(callerPlan, _minPlan))
        {
            context.Result = new ObjectResult(new
            {
                error = "plan_required",
                message = $"Esta funcionalidad requiere el plan {_minPlan} o superior. Su plan actual es {callerPlan}.",
                requiredPlan = _minPlan.ToString(),
                currentPlan  = callerPlan.ToString(),
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
