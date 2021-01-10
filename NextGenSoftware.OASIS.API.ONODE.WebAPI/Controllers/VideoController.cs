using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Config;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/video")]
    public class VideoController : OASISControllerBase
    {
        OASISSettings _settings;

        public VideoController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Start's a video call. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("StartVideoCall")]
        public ActionResult<bool> StartVideoCall()
        {
            // TODO: Finish implementing.
            return Ok();
        }
    }
}
