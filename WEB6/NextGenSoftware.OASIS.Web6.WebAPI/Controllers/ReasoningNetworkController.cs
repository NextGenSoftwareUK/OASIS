using System;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// FAHRN - the Fractal Adaptive Holonic Reasoning Network. Register reasoning agents and dispatch problems
    /// to the controller agent, which scores, routes, executes and learns from every outcome.
    /// </summary>
    [ApiController]
    [Route("v1/reasoning-network")]
    public class ReasoningNetworkController : Web6ControllerBase
    {
        /// <summary>Registers a new reasoning agent (e.g. a specific provider/model) with the network.</summary>
        [HttpPost("agents")]
        [ProducesResponseType(typeof(ReasoningAgentMetadata), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterAgent([FromBody] ReasoningAgentMetadata agent)
        {
            FAHRNManager manager = new FAHRNManager(AvatarId);
            var result = await manager.RegisterAgentAsync(agent);

            return result.IsError ? BadRequest(result) : Ok(result);
        }

        /// <summary>Lists every reasoning agent currently registered with FAHRN, with its live composite scoring metadata.</summary>
        [HttpGet("agents")]
        [ProducesResponseType(typeof(System.Collections.Generic.List<ReasoningAgentMetadata>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAgents()
        {
            FAHRNManager manager = new FAHRNManager(AvatarId);
            var result = await manager.GetRegisteredAgentsAsync();

            return result.IsError ? BadRequest(result) : Ok(result);
        }

        /// <summary>
        /// Seeds FAHRN with one reasoning agent per model in the OpenServ SERV catalog (skips any AgentName
        /// already registered), so the network can immediately score/route/braid across every OpenServ-reachable
        /// model (OpenAI, Anthropic, Google, xAI, Qwen, DeepSeek) behind a single SERV_API_KEY. Safe to call
        /// repeatedly. POST https://api.web6.oasisomniverse.one/v1/reasoning-network/agents/seed-openserv
        /// </summary>
        [HttpPost("agents/seed-openserv")]
        [ProducesResponseType(typeof(System.Collections.Generic.List<ReasoningAgentMetadata>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SeedOpenServAgents()
        {
            FAHRNManager manager = new FAHRNManager(AvatarId);
            var result = await manager.SeedDefaultOpenServAgentsAsync();

            return result.IsError ? BadRequest(result) : Ok(result);
        }

        /// <summary>
        /// Dispatches a problem to the reasoning network. The controller agent scores eligible agents, picks
        /// Serial/Parallel/Decomposed execution, runs loop detection, assembles the final Mermaid plan and
        /// updates every involved agent's score via EMA before returning.
        /// </summary>
        [HttpPost("dispatch")]
        [ProducesResponseType(typeof(DispatchResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Dispatch([FromBody] DispatchRequest request)
        {
            if (request.AvatarId == System.Guid.Empty)
                request.AvatarId = AvatarId;

            FAHRNManager manager = new FAHRNManager(request.AvatarId);
            var result = await manager.DispatchAsync(request);

            return result.IsError ? BadRequest(result) : Ok(result);
        }

        // ── Priority 24: SkillOpt endpoints ─────────────────────────────────────────

        /// <summary>Returns the current best skill document for an agent and task category.</summary>
        [HttpGet("agents/{agentId}/skills/{category}")]
        [ProducesResponseType(typeof(SkillDocument), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSkill(string agentId, string category)
        {
            var manager = new SkillOptManager(AvatarId, OASISDNA);
            var result = await manager.LoadSkillAsync(Guid.Parse(agentId), category);
            return result.IsError ? BadRequest(result) : Ok(result.Result);
        }

        /// <summary>Triggers one SkillOpt epoch — proposes and validates a textual edit to the agent's skill document.</summary>
        [HttpPost("agents/{agentId}/skills/{category}/evolve")]
        [ProducesResponseType(typeof(SkillDocument), StatusCodes.Status200OK)]
        public async Task<IActionResult> EvolveSkill(string agentId, string category)
        {
            var manager = new SkillOptManager(AvatarId, OASISDNA);
            var result = await manager.RunEpochAsync(Guid.Parse(agentId), category);
            return result.IsError ? BadRequest(result) : Ok(result.Result);
        }
    }
}
