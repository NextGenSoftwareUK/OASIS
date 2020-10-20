using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : OASISControllerBase
    {
        OASISSettings _settings;

        public FilesController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
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
