using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Config;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/seeds")]
    public class SeedsController : OASISControllerBase
    {
        OASISSettings _settings;

        public SeedsController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Reward Seeds. PREVIEW - COMING SOON...
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("RewardSeeds")]
        public ActionResult<bool> RewardSeeds()
        {
            // TODO: Finish implementing.
            return Ok();
        }
    }
}
