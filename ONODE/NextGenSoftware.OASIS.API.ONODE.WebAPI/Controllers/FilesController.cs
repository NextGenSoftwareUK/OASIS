using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : OASISControllerBase
    {
        /*
        OASISDNA _settings;

        public FilesController(IOptions<OASISDNA> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }*/

        public FilesController()
        {
            
        }

        /// <summary>
        /// Get's all files stored for the currently logged in avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-files-stored-for-current-logged-in-avatar")]
        public async Task<OASISResult<bool>> GetAllFilesStoredForCurrentLoggedInAvatar()
        {
            // TODO: Finish implementing.
            return new()
            {
                IsError = false
            };
        }
    }
}
