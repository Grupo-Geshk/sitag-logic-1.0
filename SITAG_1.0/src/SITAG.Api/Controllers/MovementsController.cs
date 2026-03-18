using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SITAG.Application.Animals.Commands;
using SITAG.Application.Animals.Queries;

namespace SITAG.Api.Controllers;

[Route("movements")]
[Authorize]
public sealed class MovementsController : ApiControllerBase
{
    /// <summary>
    /// Move a single active animal to another farm/division.
    /// Logs an AnimalMovement entry and updates the animal's location.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Move([FromBody] MoveAnimalCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// List movement records with optional filters.
    /// farmId matches either origin or destination farm.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid?           farmId,
        [FromQuery] Guid?           animalId,
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetMovementsQuery(farmId, animalId, startDate, endDate, page, pageSize), ct));
}
