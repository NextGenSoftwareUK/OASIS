using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// The WEB6 unified AI completion endpoint - one request shape, routed/normalised across every AI provider.
    /// </summary>
    [ApiController]
    [Route("v1")]
    public class CompletionController : Web6ControllerBase
    {
        /// <summary>
        /// Routes a completion request to whichever AI provider/model best fits, normalising the response.
        /// POST https://api.web6.oasisomniverse.one/v1/complete
        /// </summary>
        [HttpPost("complete")]
        [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Complete([FromBody] CompletionRequest request)
        {
            if (request.AvatarId == System.Guid.Empty)
                request.AvatarId = AvatarId;

            AIProviderManager manager = new AIProviderManager(request.AvatarId);
            var result = await manager.CompleteAsync(request);

            return result.IsError ? BadRequest(result) : Ok(result);
        }
    }
}
