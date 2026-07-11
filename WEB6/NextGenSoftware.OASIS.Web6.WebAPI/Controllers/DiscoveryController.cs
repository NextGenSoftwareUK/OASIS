using Microsoft.AspNetCore.Mvc;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// MCP and A2A discovery documents — auto-detected by MCP clients (Claude.ai, OpenAI) and Google A2A agents.
    /// </summary>
    [ApiController]
    public class DiscoveryController : ControllerBase
    {
        /// <summary>MCP discovery document. Tells MCP clients where the /mcp endpoint is and what it offers.</summary>
        [HttpGet(".well-known/mcp.json")]
        public IActionResult McpDiscovery() => Ok(new
        {
            schema_version = "v1",
            name_for_human = "OASIS WEB4–WEB10",
            name_for_model = "oasis",
            description_for_human = "Universal AI abstraction layer — Web4 identity/data, Web5 apps/quests, Web6 AI/FAHRN, Web7 symbiosis, Web8 mesh, Web9 singularity, Web10 source.",
            description_for_model = "Access all OASIS capabilities: avatar/karma (web4), quests/missions/OAPPs (web5), AI completion/FAHRN dispatch/holonic-braid (web6), bio-signal symbiosis (web7), galactic mesh routing (web8), unified status (web9), root identity (web10).",
            auth = new { type = "bearer" },
            api = new { type = "mcp", url = "https://api.web6.oasisomniverse.one/mcp" }
        });

        /// <summary>Google A2A agent card. Describes this OASIS agent and its skills to peer A2A agents.</summary>
        [HttpGet(".well-known/agent.json")]
        public IActionResult A2AAgentCard() => Ok(new
        {
            name = "OASIS WEB6 FAHRN",
            description = "Fractal Adaptive Holonic Reasoning Network — universal AI abstraction and aggregation layer (Web4–Web10)",
            url = "https://api.web6.oasisomniverse.one",
            version = "2.0.0",
            documentationUrl = "https://web6.oasisomniverse.one",
            capabilities = new { streaming = true, pushNotifications = false, stateTransitionHistory = false },
            defaultInputModes = new[] { "text/plain", "application/json" },
            defaultOutputModes = new[] { "text/plain", "application/json" },
            skills = new object[]
            {
                new { id = "fahrn-solve",    name = "FAHRN Solve",    description = "Full pipeline: classify → avatar context → dispatch → BRAID → answer + reasoning trace",      inputModes = new[] { "text/plain" },        outputModes = new[] { "application/json" } },
                new { id = "ai-complete",    name = "AI Completion",  description = "Route chat completions across 15+ AI providers with automatic failover",                        inputModes = new[] { "application/json" },  outputModes = new[] { "application/json" } },
                new { id = "holonic-braid",  name = "Holonic BRAID",  description = "Shared reasoning graph memory — lookup or create Mermaid execution graphs per task type",      inputModes = new[] { "application/json" },  outputModes = new[] { "application/json" } },
                new { id = "oasis-data",     name = "OASIS Data",     description = "Avatar, karma, wallet, NFT, holon CRUD via COSMIC ORM across 40+ storage providers" }
            }
        });
    }
}
