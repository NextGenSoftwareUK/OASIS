using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : OASISControllerBase
    {
        OASISDNA _settings;

        public FilesController(IOptions<OASISDNA> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Get's all files stored for the currently logged in avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllFilesStoredForCurrentLoggedInAvatar")]
        public ActionResult<bool> GetAllFilesStoredForCurrentLoggedInAvatar()
        {
            // TODO: Finish implementing.
            return Ok();
        }
    }
}
