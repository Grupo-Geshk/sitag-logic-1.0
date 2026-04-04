using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Farms.Commands;
using SITAG.Application.Farms.Dtos;
using SITAG.Application.Farms.Queries;

namespace SITAG.Api.Controllers;

[Route("farms")]
[Authorize]
public sealed class FarmsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await Sender.Send(new GetFarmsQuery(), ct));

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct) =>
        Ok(await Sender.Send(new GetFarmsOverviewQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetFarmByIdQuery(id), ct));

    [HttpGet("{id:guid}/detail")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetFarmDetailQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFarmCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFarmRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new UpdateFarmCommand(id, body.Name, body.Location, body.Hectares, body.FarmType, body.IsOwned), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteFarmCommand(id), ct);
        return NoContent();
    }

    // ── Brands (hierros) ─────────────────────────────────────────────────────
    [HttpGet("{farmId:guid}/hierros")]
    public async Task<IActionResult> GetBrands(Guid farmId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetFarmBrandsQuery(farmId), ct));

    [HttpPost("{farmId:guid}/hierros")]
    public async Task<IActionResult> CreateBrand(Guid farmId, [FromBody] FarmBrandRequest body, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateFarmBrandCommand(farmId, body.Name, body.PhotoUrl), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{farmId:guid}/hierros/{brandId:guid}")]
    public async Task<IActionResult> UpdateBrand(Guid farmId, Guid brandId, [FromBody] FarmBrandRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new UpdateFarmBrandCommand(brandId, body.Name, body.PhotoUrl), ct));

    [HttpDelete("{farmId:guid}/hierros/{brandId:guid}")]
    public async Task<IActionResult> DeleteBrand(Guid farmId, Guid brandId, CancellationToken ct)
    {
        await Sender.Send(new DeleteFarmBrandCommand(brandId), ct);
        return NoContent();
    }

    // ── Divisions ────────────────────────────────────────────────────────────
    [HttpGet("{farmId:guid}/divisions")]
    public async Task<IActionResult> GetDivisions(Guid farmId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetDivisionsQuery(farmId), ct));

    [HttpGet("divisions/{divisionId:guid}")]
    public async Task<IActionResult> GetDivisionById(Guid divisionId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetDivisionByIdQuery(divisionId), ct));

    [HttpPost("{farmId:guid}/divisions")]
    public async Task<IActionResult> CreateDivision(Guid farmId, [FromBody] CreateDivisionRequest body, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateDivisionCommand(farmId, body.Name, body.MaxCapacity), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("divisions/{divisionId:guid}")]
    public async Task<IActionResult> UpdateDivision(Guid divisionId, [FromBody] UpdateDivisionRequest body, CancellationToken ct) =>
        Ok(await Sender.Send(new UpdateDivisionCommand(divisionId, body.Name, body.MaxCapacity), ct));

    [HttpDelete("divisions/{divisionId:guid}")]
    public async Task<IActionResult> DeleteDivision(Guid divisionId, CancellationToken ct)
    {
        await Sender.Send(new DeleteDivisionCommand(divisionId), ct);
        return NoContent();
    }
}

public sealed record UpdateFarmRequest(string Name, string? Location, decimal? Hectares, string? FarmType, bool IsOwned);
public sealed record FarmBrandRequest(string Name, string? PhotoUrl);
public sealed record CreateDivisionRequest(string Name, int? MaxCapacity);
public sealed record UpdateDivisionRequest(string Name, int? MaxCapacity);
