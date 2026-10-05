using Microsoft.AspNetCore.Mvc;
using System;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/share")]
    public class ShareController : OASISControllerBase
    {
        //  OASISSettings _settings;

        //public ShareController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        //{
        //    _settings = OASISSettings.Value;
        //}

        public ShareController()
        {
            
        }

        /// <summary>
        /// Share a given holon with a given avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <param name="holonId"></param>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("share-holon/{holonId}/{avatarId}")]
        public OASISResult<bool> ShareHolon(Guid holonId, Guid avatarId)
        {
            // TODO: Finish implementing.
            return new();
        }

        /// <summary>
        /// Share a given holon with a groups of avatars. PREVIEW - COMING SOON...
        /// </summary>
        /// <param name="holonId"></param>
        /// <param name="avatarIds"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("share-holon/{holonId}/{avatarIds}")]
        public OASISResult<bool> ShareHolon(Guid holonId, Guid[] avatarIds)
        {
            // TODO: Finish implementing.
            return new();
        }
    }
}
