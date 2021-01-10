using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Config;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/oapp")]
    public class OAPPController : OASISControllerBase
    {
        OASISSettings _settings;

        public OAPPController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Get's all OAPP's installed for the current logged in avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllOAPPsInstalledForCurrentLoggedInAvatar")]
        public ActionResult<bool> GetAllOAPPsInstalledForCurrentLoggedInAvatar()
        {
            // TODO: Finish implementing.
            return Ok();
        }

    }
}
