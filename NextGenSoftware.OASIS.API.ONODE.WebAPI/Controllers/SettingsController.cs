using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : OASISControllerBase
    {
        //OASISSettings _settings;

        //public SettingsController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        //{
        //    _settings = OASISSettings.Value;
        //}

        public SettingsController()
        {
            //_settings = OASISSettings.Value;
        }

        /// <summary>
        /// Return's all settings for the currently logged in avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllSettingsForCurrentLoggedInAvatar")]
        public OASISResult<bool> GetAllSettingsForCurrentLoggedInAvatar()
        {
            // TODO: Finish implementing.
            return new();
        }
    }
}
