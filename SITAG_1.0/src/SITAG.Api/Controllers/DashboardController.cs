using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Dashboard.Queries;

namespace SITAG.Api.Controllers;

[Route("dashboard")]
[Authorize]
public sealed class DashboardController : ApiControllerBase
{
    /// <summary>
    /// Producer KPIs: animal counts, economic summary for current month,
    /// and animal distribution by farm (REQ-DASH-01/02).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await Sender.Send(new GetDashboardQuery(), ct));

    /// <summary>
    /// Dynamic operational alerts: sick animals, low stock, expiring supplies (REQ-DASH-03).
    /// Generated on demand — not persisted.
    /// </summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts(CancellationToken ct) =>
        Ok(await Sender.Send(new GetDashboardAlertsQuery(), ct));
}
