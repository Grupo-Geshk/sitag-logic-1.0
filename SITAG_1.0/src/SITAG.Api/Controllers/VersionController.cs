using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace SITAG.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class VersionController : ControllerBase
{
    /// <summary>
    /// Returns the assembly informational version (set via InformationalVersion in the csproj).
    /// Useful for confirming which build is running in Railway.
    /// </summary>
    [HttpGet("/version")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var version = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

        return Ok(new { version });
    }
}
