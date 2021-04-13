using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/gifts")]
    public class GiftsController : OASISControllerBase
    {
        OASISDNA _settings;

        public GiftsController(IOptions<OASISDNA> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Get's all gifts stored for the currently logged in avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllGiftsForCurrentLoggedInAvatar")]
        public ActionResult<bool> GetAllGiftsForCurrentLoggedInAvatar()
        {
            // TODO: Finish implementing.
            return Ok();
        }
    }
}
