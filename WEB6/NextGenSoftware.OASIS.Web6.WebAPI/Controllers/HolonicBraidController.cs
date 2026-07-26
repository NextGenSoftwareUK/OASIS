using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// The Holonic BRAID shared reasoning-graph library - read and seed Mermaid execution graphs per task type.
    /// </summary>
    [ApiController]
    [Route("v1/holonic-braid")]
    public class HolonicBraidController : Web6ControllerBase
    {
        /// <summary>Looks up the shared reasoning graph already generated for a task type, if any.</summary>
        [HttpGet("graph/{taskType}")]
        [ProducesResponseType(typeof(HolonicBraidGraphDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGraph(string taskType)
        {
            HolonicBraidManager manager = new HolonicBraidManager(AvatarId);
            var result = await manager.FindGraphForTaskTypeAsync(taskType);

            if (result.IsError)
                return BadRequest(result);

            return result.Result == null ? NotFound(result) : Ok(result);
        }

        /// <summary>Seeds the shared library with a reasoning graph for a task type (the "generator" step of the two-stage BRAID protocol).</summary>
        [HttpPost("graph/{taskType}")]
        [ProducesResponseType(typeof(HolonicBraidGraphDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveGraph(string taskType, [FromBody] SaveGraphRequest request)
        {
            HolonicBraidManager manager = new HolonicBraidManager(AvatarId);
            var result = await manager.SaveGraphAsync(taskType, request.MermaidDiagram, request.GeneratedByModel);

            return result.IsError ? BadRequest(result) : Ok(result);
        }
    }

    public class SaveGraphRequest
    {
        public string MermaidDiagram { get; set; }
        public string GeneratedByModel { get; set; }
    }
}
