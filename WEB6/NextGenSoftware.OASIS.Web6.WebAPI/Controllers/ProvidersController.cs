using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// Live health/latency per AI provider. Backed by ProviderHealthMonitor which pings each provider every 60 seconds.
    /// </summary>
    [ApiController]
    [Route("v1/providers")]
    public class ProvidersController : Web6ControllerBase
    {
        /// <summary>
        /// Returns the last-known health and latency for every configured AI provider.
        /// Results are cached for 60 seconds; pass ?refresh=true to force an immediate re-ping.
        /// GET https://api.web6.oasisomniverse.one/v1/providers/status
        /// </summary>
        [HttpGet("status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Status([FromQuery] bool refresh = false)
        {
            ProviderHealthMonitor monitor = new ProviderHealthMonitor(AvatarId, OASISDNA);
            var status = await monitor.GetStatusAsync(forceRefresh: refresh);
            return Ok(status);
        }
    }
}
