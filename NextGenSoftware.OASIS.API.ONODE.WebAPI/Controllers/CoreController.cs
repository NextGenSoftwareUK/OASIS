using Microsoft.AspNetCore.Mvc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/core")]
    public class CoreController : OASISControllerBase
    {
        //OASISSettings _settings;

        //public CoreController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        public CoreController()
        {
          //  _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Generate a new Moon (OAPP) PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("GenerateMoon")]
        public ActionResult<bool> GenerateMoon()
        {
            // TODO: Finish implementing.
            return Ok();
        }
    }
}
