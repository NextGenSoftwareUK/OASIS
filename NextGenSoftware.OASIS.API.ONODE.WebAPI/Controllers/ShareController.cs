using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;

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
        [HttpGet("ShareHolon/{holonId}/{avatarId}")]
        public ActionResult<bool> ShareHolon(Guid holonId, Guid avatarId)
        {
            // TODO: Finish implementing.
            return Ok();
        }

        /// <summary>
        /// Share a given holon with a groups of avatars. PREVIEW - COMING SOON...
        /// </summary>
        /// <param name="holonId"></param>
        /// <param name="avatarIds"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ShareHolon/{holonId}/{avatarIds}")]
        public ActionResult<bool> ShareHolon(Guid holonId, Guid[] avatarIds)
        {
            // TODO: Finish implementing.
            return Ok();
        }
    }
}
