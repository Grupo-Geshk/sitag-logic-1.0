using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Animals.Queries;
using SITAG.Domain.Enums;

namespace SITAG.Api.Controllers;

[Route("animalevents")]
[Authorize]
public sealed class AnimalEventsController : ApiControllerBase
{
    /// <summary>
    /// Cross-animal event list with optional filters (REQ-EVENT-01).
    /// Supports farmId, animalId, eventType, startDate, endDate, pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid?            farmId,
        [FromQuery] Guid?            animalId,
        [FromQuery] AnimalEventType? eventType,
        [FromQuery] DateTimeOffset?  startDate,
        [FromQuery] DateTimeOffset?  endDate,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(
            new GetAnimalEventsFilteredQuery(farmId, animalId, eventType, startDate, endDate, page, pageSize), ct));
}
