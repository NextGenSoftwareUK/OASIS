using Microsoft.AspNetCore.Mvc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/oapp")]
    public class OAPPController : OASISControllerBase
    {
        //OASISSettings _settings;

        //public OAPPController(IOptions<OASISDNA> OASISSettings) : base(OASISSettings)
        //{
        //    _settings = OASISSettings.Value;
        //}

        public OAPPController()
        {
            
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
