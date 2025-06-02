using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

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
        /// Generate a new Moon (OApp) PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate-moon")]
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
