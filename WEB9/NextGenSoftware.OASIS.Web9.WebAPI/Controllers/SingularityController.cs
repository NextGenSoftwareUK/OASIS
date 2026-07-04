using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web9.Core.Managers;
using NextGenSoftware.OASIS.Web9.Core.Models;
using NextGenSoftware.OASIS.Web9.WebAPI.Attributes;

namespace NextGenSoftware.OASIS.Web9.WebAPI.Controllers
{
    /// <summary>The Singularity Layer - one unified view across every other OASIS layer, live.</summary>
    [Authorize]
    [ApiController]
    [Route("v1/singularity")]
    public class SingularityController : ControllerBase
    {
        private static readonly SingularityAggregationManager _manager = new SingularityAggregationManager();

        /// <summary>Probes WEB4-WEB8 in parallel and returns one unified status report - "the network observing itself".</summary>
        [HttpGet("status")]
        [ProducesResponseType(typeof(UnifiedStatusReport), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnifiedStatus()
        {
            UnifiedStatusReport report = await _manager.GetUnifiedStatusAsync();
            return Ok(report);
        }
    }
}
