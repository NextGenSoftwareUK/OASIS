using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/gifts")]
    public class GiftsController : OASISControllerBase
    {
        public GiftsController() : base()
        {
        }

        /// <summary>
        /// Get's all gifts stored for the currently logged in avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-gifts-for-current-logged-in-avatar")]
        public OASISResult<bool> GetAllGiftsForCurrentLoggedInAvatar()
        {
            // TODO: Finish implementing.
            return new();
        }
    }
}
