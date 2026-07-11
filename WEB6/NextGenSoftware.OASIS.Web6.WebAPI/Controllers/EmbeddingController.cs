using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// Generates float embeddings for one or more texts via OpenAI, Cohere, or HuggingFace.
    /// POST /v1/embed
    /// </summary>
    [ApiController]
    [Route("v1")]
    public class EmbeddingController : Web6ControllerBase
    {
        /// <summary>
        /// Generates embeddings for one or more texts via the configured provider.
        /// Returns float arrays suitable for semantic search, RAG pipelines, or cosine-similarity comparisons.
        /// POST https://api.web6.oasisomniverse.one/v1/embed
        /// </summary>
        [HttpPost("embed")]
        [ProducesResponseType(typeof(EmbeddingResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Embed([FromBody] EmbeddingRequest request)
        {
            EmbeddingManager manager = new EmbeddingManager(AvatarId, OASISDNA);
            var result = await manager.EmbedAsync(request);
            return result.IsError ? BadRequest(result) : Ok(result);
        }
    }
}
