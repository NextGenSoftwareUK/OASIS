using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// The FAHRN hero endpoint — the full pipeline in one call.
    /// POST /v1/fahrn/solve: classify → avatar context → BRAID → dispatch → answer + trace + telemetry.
    /// </summary>
    [ApiController]
    [Route("v1/fahrn")]
    public class FahrnSolveController : Web6ControllerBase
    {
        /// <summary>
        /// Runs the full FAHRN pipeline: auto-classify task type, inject avatar context (Web4+Web5),
        /// look up Holonic BRAID graph, dispatch to the reasoning network, and return the answer with
        /// full reasoning trace and telemetry in one call.
        /// POST https://api.web6.oasisomniverse.one/v1/fahrn/solve
        /// </summary>
        [HttpPost("solve")]
        [ProducesResponseType(typeof(FahrnSolveResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Solve([FromBody] FahrnSolveRequest request)
        {
            if (request.AvatarId == System.Guid.Empty)
                request.AvatarId = AvatarId;

            FahrnSolveManager manager = new FahrnSolveManager(AvatarId, OASISDNA);
            var result = await manager.SolveAsync(request);

            return result.IsError ? BadRequest(result) : Ok(result);
        }
    }
}
