using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Producers.Commands;
using SITAG.Application.Producers.Queries;

namespace SITAG.Api.Controllers;

/// <summary>
/// Producer profile for the authenticated tenant (DATABASE_MODEL.md §4.1).
/// </summary>
[Route("productor")]
[Authorize]
public sealed class ProducerController : ApiControllerBase
{
    /// <summary>
    /// Get the producer profile of the current tenant.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await Sender.Send(new GetProducerQuery(), ct));

    /// <summary>
    /// Update the producer's display name.
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> Update([FromBody] UpdateProducerCommand cmd, CancellationToken ct) =>
        Ok(await Sender.Send(cmd, ct));
}
