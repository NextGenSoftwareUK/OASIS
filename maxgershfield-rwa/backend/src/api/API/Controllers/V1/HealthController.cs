namespace API.Controllers.V1;

/// <summary>
/// Controller responsible for health check operations. 
/// Primarily used to check if the API is responsive and operational.
/// </summary>
[Route($"{ApiAddresses.Base}")]
[AllowAnonymous]
public sealed class HealthController : V1BaseController
{
    /// <summary>
    /// Simple ping endpoint used to verify if the API is running and responsive.
    /// This method responds with a basic "pong" message to confirm the service's availability.
    /// Typically used in load balancers or service monitors for uptime checks.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> indicating that the service is healthy with a "pong" response.</returns>
    [HttpGet("ping")]
    public Task<IActionResult> Ping() => Task.FromResult<IActionResult>(result: Ok("pong"));
}