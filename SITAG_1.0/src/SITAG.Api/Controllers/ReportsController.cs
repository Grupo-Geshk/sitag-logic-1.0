using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Animals.Queries;

namespace SITAG.Api.Controllers;

[Route("reports")]
[Authorize]
public sealed class ReportsController : ApiControllerBase
{
    /// <summary>
    /// Full chronological timeline for an animal:
    /// events (vaccinations, treatments, weight records, etc.) and
    /// location movements, merged and sorted descending by date.
    /// </summary>
    [HttpGet("animal-timeline/{animalId:guid}")]
    public async Task<IActionResult> AnimalTimeline(Guid animalId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetAnimalTimelineQuery(animalId), ct));
}
