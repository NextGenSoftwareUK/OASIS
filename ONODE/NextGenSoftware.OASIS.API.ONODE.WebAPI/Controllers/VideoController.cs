using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/video")]
    public class VideoController : OASISControllerBase
    {
        //OASISSettings _settings;

        //public VideoController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        //{
        //    _settings = OASISSettings.Value;
        //}

        public VideoController()
        {
            //_settings = OASISSettings.Value;
        }

        /// <summary>
        /// Start's a video call. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("start-video-call")]
        public OASISResult<bool> StartVideoCall()
        {
            // TODO: Finish implementing.
            return new();
        }
    }
}
