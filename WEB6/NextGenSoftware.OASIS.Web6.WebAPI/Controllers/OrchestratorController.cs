using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// Aggregates every agent protocol/orchestration framework (MCP, A2A, LangChain, AutoGen, CrewAI, Semantic
    /// Kernel, or any generic webhook) behind WEB6's unified interface. Register an external orchestrator once,
    /// then invoke it the same way you'd call any AI provider through /v1/complete.
    /// </summary>
    [ApiController]
    [Route("v1/orchestrators")]
    public class OrchestratorController : Web6ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(OrchestratorAdapterConfig), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterAdapter([FromBody] OrchestratorAdapterConfig config)
        {
            OrchestratorManager manager = new OrchestratorManager(AvatarId);
            var result = await manager.RegisterAdapterAsync(config);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(System.Collections.Generic.List<OrchestratorAdapterConfig>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdapters()
        {
            OrchestratorManager manager = new OrchestratorManager(AvatarId);
            var result = await manager.GetAdaptersAsync();
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpPost("invoke")]
        [ProducesResponseType(typeof(OrchestratorInvokeResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Invoke([FromBody] OrchestratorInvokeRequest request)
        {
            OrchestratorManager manager = new OrchestratorManager(AvatarId);
            var result = await manager.InvokeAsync(request);
            return result.IsError ? BadRequest(result) : Ok(result);
        }
    }
}
