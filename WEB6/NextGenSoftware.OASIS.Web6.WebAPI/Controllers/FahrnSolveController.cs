using System;
using System.Collections.Generic;
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
            if (request.AvatarId == Guid.Empty)
                request.AvatarId = AvatarId;

            FahrnSolveManager manager = new FahrnSolveManager(AvatarId, OASISDNA);
            var result = await manager.SolveAsync(request);

            return result.IsError ? BadRequest(result) : Ok(result);
        }

        /// <summary>
        /// Dry-run budget estimate — returns projected token count and cost for a DispatchRequest
        /// using the model pricing table, without making any actual agent calls.
        /// GET https://api.web6.oasisomniverse.one/v1/fahrn/budget-estimate
        /// </summary>
        [HttpGet("budget-estimate")]
        [ProducesResponseType(typeof(BudgetEstimateResponse), StatusCodes.Status200OK)]
        public IActionResult BudgetEstimate([FromQuery] string taskType = "general", [FromQuery] string mode = "auto", [FromQuery] int agentCount = 1)
        {
            // Approximate pricing per 1K tokens (input+output combined) for common providers
            var pricing = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["gpt-4o"]                    = 0.0075m,
                ["gpt-4o-mini"]               = 0.00030m,
                ["claude-opus-4-8"]           = 0.0750m,
                ["claude-sonnet-5"]           = 0.0150m,
                ["claude-haiku-4-5"]          = 0.00125m,
                ["gemini-2.0-flash"]          = 0.00035m,
                ["llama-3.3-70b-versatile"]   = 0.00059m,
                ["auto"]                      = 0.0075m,   // fallback estimate
            };

            // Rough token estimate per agent call by task type
            int estimatedTokensPerAgent = taskType?.ToLowerInvariant() switch
            {
                "mathematics" or "reasoning" => 2000,
                "legal" or "architecture"    => 3000,
                "code"                       => 2500,
                "real-time"                  => 500,
                _                            => 1500,
            };

            // Mode multiplier: Serial=1 agent, Parallel/Voting=all agents, Debate=3 steps, Decomposed=up to 3
            int totalCalls = mode?.ToLowerInvariant() switch
            {
                "serial"      => 1,
                "debate"      => Math.Min(agentCount, 3),
                "decomposed"  => Math.Min(agentCount, OASISDNA?.OASIS?.Web6?.FAHRN?.MaxDecomposedSubProblems ?? 3),
                _             => agentCount,  // parallel, voting, auto
            };

            int totalTokens = estimatedTokensPerAgent * totalCalls;
            decimal pricePerK = pricing.TryGetValue("auto", out decimal p) ? p : 0.0075m;
            decimal estimatedCostUsd = Math.Round((totalTokens / 1000m) * pricePerK, 4);

            return Ok(new BudgetEstimateResponse
            {
                TaskType              = taskType,
                ModeAssumed           = mode,
                AgentCount            = agentCount,
                EstimatedTokensPerAgent = estimatedTokensPerAgent,
                EstimatedTotalTokens  = totalTokens,
                EstimatedCostUsd      = estimatedCostUsd,
                PricingNote           = "Estimates use mid-tier model pricing. Actual cost varies by provider, model, and prompt length."
            });
        }
    }

    public class BudgetEstimateResponse
    {
        public string  TaskType               { get; set; }
        public string  ModeAssumed            { get; set; }
        public int     AgentCount             { get; set; }
        public int     EstimatedTokensPerAgent { get; set; }
        public int     EstimatedTotalTokens   { get; set; }
        public decimal EstimatedCostUsd       { get; set; }
        public string  PricingNote            { get; set; }
    }
}
