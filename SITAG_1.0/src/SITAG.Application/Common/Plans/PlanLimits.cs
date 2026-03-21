using SITAG.Domain.Enums;

namespace SITAG.Application.Common.Plans;

/// <summary>
/// Defines resource limits and feature access per tenant plan.
/// Active animal count = only AnimalStatus.Activo (Vendido / Muerto are excluded).
/// </summary>
public static class PlanLimits
{
    public const int Unlimited = int.MaxValue;

    // ── Resource caps ────────────────────────────────────────────────────────

    public static int MaxActiveAnimals(TenantPlan plan) => plan switch
    {
        TenantPlan.Semilla      => 15,
        TenantPlan.Crecimiento  => 100,
        TenantPlan.Profesional  => 300,
        TenantPlan.Corporativo  => Unlimited,
        _                       => 15
    };

    public static int MaxFarms(TenantPlan plan) => plan switch
    {
        TenantPlan.Semilla      => 1,
        TenantPlan.Crecimiento  => 2,
        TenantPlan.Profesional  => 5,
        TenantPlan.Corporativo  => Unlimited,
        _                       => 1
    };

    public static int MaxUsers(TenantPlan plan) => plan switch
    {
        TenantPlan.Semilla      => 1,
        TenantPlan.Crecimiento  => 1,
        TenantPlan.Profesional  => Unlimited,
        TenantPlan.Corporativo  => Unlimited,
        _                       => 1
    };

    // ── Feature access ───────────────────────────────────────────────────────

    /// <summary>Returns true if the plan meets or exceeds the minimum required plan.</summary>
    public static bool CanAccess(TenantPlan plan, TenantPlan requiredMinPlan)
        => (int)plan >= (int)requiredMinPlan;
}
