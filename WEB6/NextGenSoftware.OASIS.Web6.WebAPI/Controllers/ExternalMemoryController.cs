using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Memory;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// REST endpoints for external memory providers (Mem0, Zep, Letta, LangMem, Graphiti).
    /// Priority 15 — External Memory Providers.
    /// </summary>
    [ApiController]
    [Route("v1/memory/external")]
    public class ExternalMemoryController : Web6ControllerBase
    {
        /// <summary>
        /// Lists all external memory providers registered and configured via environment variables.
        /// GET /v1/memory/external/providers
        /// </summary>
        [HttpGet("providers")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public IActionResult GetProviders()
        {
            return Ok(MemoryProviderManager.Instance.ProviderNames);
        }

        /// <summary>
        /// Searches one or more external memory providers for memories relevant to the query.
        /// POST /v1/memory/external/search
        /// </summary>
        [HttpPost("search")]
        [ProducesResponseType(typeof(List<ExternalMemorySearchResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromBody] ExternalMemorySearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Query))
                return BadRequest(new { error = "query is required" });

            Guid avatarId = request.AvatarId != Guid.Empty ? request.AvatarId : AvatarId;

            var results = await MemoryProviderManager.Instance.SearchAllAsync(avatarId, request.Query, request.Providers, request.TopK ?? 5);
            return Ok(results);
        }

        /// <summary>
        /// Adds a memory to the specified external memory provider.
        /// POST /v1/memory/external/add
        /// </summary>
        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Add([FromBody] ExternalMemoryAddRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Content))
                return BadRequest(new { error = "content is required" });
            if (string.IsNullOrWhiteSpace(request?.Provider))
                return BadRequest(new { error = "provider is required" });

            Guid avatarId = request.AvatarId != Guid.Empty ? request.AvatarId : AvatarId;

            var provider = MemoryProviderManager.Instance.Get(request.Provider);
            if (provider == null)
                return NotFound(new { error = $"Provider '{request.Provider}' is not registered" });

            await provider.AddAsync(avatarId, request.Content, request.Metadata);
            return NoContent();
        }

        /// <summary>
        /// Deletes a specific memory entry from an external memory provider.
        /// DELETE /v1/memory/external/{provider}/{id}
        /// </summary>
        [HttpDelete("{provider}/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(string provider, string id)
        {
            var p = MemoryProviderManager.Instance.Get(provider);
            if (p == null)
                return NotFound(new { error = $"Provider '{provider}' is not registered" });

            await p.DeleteAsync(AvatarId, id);
            return NoContent();
        }
    }

    public class ExternalMemorySearchRequest
    {
        public string Query { get; set; }
        public Guid AvatarId { get; set; }
        public List<string> Providers { get; set; }
        public int? TopK { get; set; }
    }

    public class ExternalMemoryAddRequest
    {
        public string Provider { get; set; }
        public string Content { get; set; }
        public Guid AvatarId { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}
