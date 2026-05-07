using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Import.Commands;
using SITAG.Application.Import.Queries;

namespace SITAG.Api.Controllers;

[Route("import-drafts")]
[Authorize]
public sealed class ImportDraftsController : ApiControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var draft = await Sender.Send(new GetActiveDraftQuery(), ct);
        return draft is null ? NotFound() : Ok(draft);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrReplace(
        [FromBody] CreateOrReplaceImportDraftCommand cmd, CancellationToken ct)
    {
        var draft = await Sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetActive), draft);
    }

    [HttpPut("{id:guid}/rows")]
    public async Task<IActionResult> UpdateRows(
        Guid id, [FromBody] UpdateRowsRequest req, CancellationToken ct)
    {
        var draft = await Sender.Send(new UpdateImportDraftRowsCommand(id, req.RowsJson), ct);
        return Ok(draft);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await Sender.Send(new CancelImportDraftCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new ConfirmImportDraftCommand(id), ct);
        return Ok(result);
    }
}

public sealed record UpdateRowsRequest(string RowsJson);
