using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;

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
        public OASISResult<bool> GenerateMoon()
        {
            // TODO: Finish implementing.
            return new ()
            {
                IsError = false,
                Result = true
            };
        }
    }
}
