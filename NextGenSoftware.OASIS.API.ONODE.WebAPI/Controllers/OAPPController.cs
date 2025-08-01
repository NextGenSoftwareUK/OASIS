using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

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
        /// Get's all OApp's installed for the current logged in avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-oapps-installed-for-current-logged-in-avatar")]
        public OASISResult<bool> GetAllOAPPsInstalledForCurrentLoggedInAvatar()
        {
            // TODO: Finish implementing.
            return new();
        }

    }
}
