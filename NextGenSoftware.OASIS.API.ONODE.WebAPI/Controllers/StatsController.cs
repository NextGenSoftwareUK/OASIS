using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Config;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/stats")]
    public class StatsController : OASISControllerBase
    {
        //OASISSettings _settings;

        //public StatsController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        //{
        //    _settings = OASISSettings.Value;
        //}

        public StatsController()
        {
            //_settings = OASISSettings.Value;
        }

        /// <summary>
        /// Get's the stats for the currently logged in avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetStatsForCurrentLoggedInAvatar")]
        public ActionResult<bool> GetStatsForCurrentLoggedInAvatar()
        {
            // TODO: Finish implementing.
            return Ok();
        }

    }
}
