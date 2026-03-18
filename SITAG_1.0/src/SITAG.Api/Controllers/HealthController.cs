using Microsoft.AspNetCore.Mvc;

namespace SITAG.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly IHostEnvironment _env;

    public HealthController(IHostEnvironment env) => _env = env;

    /// <summary>
    /// Quick liveness check. Returns the current environment and server UTC time.
    /// Used to verify the API starts correctly in Railway and Development.
    /// </summary>
    [HttpGet("/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() =>
        Ok(new
        {
            status = "healthy",
            environment = _env.EnvironmentName,
            serverTimeUtc = DateTimeOffset.UtcNow
        });
}
