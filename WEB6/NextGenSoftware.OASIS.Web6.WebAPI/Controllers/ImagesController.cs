using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>The WEB6 unified image-generation endpoint - the image-modality counterpart to /v1/complete.</summary>
    [ApiController]
    [Route("v1/images")]
    public class ImagesController : Web6ControllerBase
    {
        /// <summary>
        /// Generates an image via the requested provider (StabilityAI or OpenAI).
        /// POST https://api.web6.oasisomniverse.one/v1/images/generate
        /// </summary>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(ImageGenerationResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Generate([FromBody] ImageGenerationRequest request)
        {
            AIProviderManager manager = new AIProviderManager(AvatarId);
            var result = await manager.GenerateImageAsync(request);

            return result.IsError ? BadRequest(result) : Ok(result);
        }
    }
}
