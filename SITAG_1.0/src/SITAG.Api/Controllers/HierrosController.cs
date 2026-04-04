using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Farms.Commands;
using SITAG.Application.Farms.Queries;

namespace SITAG.Api.Controllers;

[Route("hierros")]
[Authorize]
public sealed class HierrosController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await Sender.Send(new GetAllBrandsQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HierroRequest body, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateFarmBrandCommand(body.Name, body.PhotoUrl), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] HierroRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new UpdateFarmBrandCommand(id, body.Name, body.PhotoUrl), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteFarmBrandCommand(id), ct);
        return NoContent();
    }
}

public sealed record HierroRequest(string Name, string? PhotoUrl);
