using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// The Holonic BRAID fractal memory hierarchy - Session → Agent → User → Group → Neighbourhood → District →
    /// City → County → Country → Continent → Earth. Create/get holons at any level, set the membrane rule that
    /// governs what propagates upward, record memory, and trigger propagation one hop at a time.
    /// </summary>
    [ApiController]
    [Route("v1/holonic-memory")]
    public class HolonicMemoryController : Web6ControllerBase
    {
        [HttpGet("earth")]
        [ProducesResponseType(typeof(HolonicMemoryHolonDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEarthHolon()
        {
            HolonicMemoryManager manager = new HolonicMemoryManager(AvatarId);
            var result = await manager.GetOrCreateEarthHolonAsync();
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpPost("holons")]
        [ProducesResponseType(typeof(HolonicMemoryHolonDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrCreateHolon([FromQuery] HolonicMemoryLevel level, [FromQuery] string name, [FromQuery] Guid parentHolonId)
        {
            HolonicMemoryManager manager = new HolonicMemoryManager(AvatarId);
            var result = await manager.GetOrCreateHolonAsync(level, name, parentHolonId);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpPut("holons/{holonId}/membrane-rule")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> SetMembraneRule(Guid holonId, [FromBody] MembraneRule rule)
        {
            HolonicMemoryManager manager = new HolonicMemoryManager(AvatarId);
            var result = await manager.SetMembraneRuleAsync(holonId, rule);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpPost("holons/{holonId}/memory")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> RecordMemory(Guid holonId, [FromBody] HolonicMemoryItem item)
        {
            HolonicMemoryManager manager = new HolonicMemoryManager(AvatarId);
            var result = await manager.RecordMemoryAsync(holonId, item);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        /// <summary>Propagates whatever the child holon's membrane rule permits up to its parent (a single hop).</summary>
        [HttpPost("holons/{childHolonId}/propagate")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> Propagate(Guid childHolonId)
        {
            HolonicMemoryManager manager = new HolonicMemoryManager(AvatarId);
            var result = await manager.PropagateAsync(childHolonId);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        /// <summary>
        /// Semantic search across all memory items in a holon.
        /// Priority 16b — returns the top-K items most similar to the query string using cosine similarity
        /// over stored embedding vectors (falls back to keyword overlap when no embeddings are stored).
        /// GET /v1/holonic-memory/holons/{holonId}/memory/search?q=...&amp;topK=5&amp;provider=auto
        /// </summary>
        [HttpGet("holons/{holonId}/memory/search")]
        [ProducesResponseType(typeof(System.Collections.Generic.List<MemorySearchResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchMemory(Guid holonId, [FromQuery] string q, [FromQuery] int topK = 5, [FromQuery] string provider = "auto")
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { error = "q (query) is required" });
            HolonicMemoryManager manager = new HolonicMemoryManager(AvatarId, OASISDNA);
            var result = await manager.QueryMemoryAsync(holonId, q, topK, provider);
            return result.IsError ? BadRequest(result) : Ok(result);
        }
    }
}
