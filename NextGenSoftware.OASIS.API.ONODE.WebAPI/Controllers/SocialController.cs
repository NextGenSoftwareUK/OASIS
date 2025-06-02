using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/social")]
    public class SocialController : OASISControllerBase
    {
        //OASISSettings _settings;

        //public SocialController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        //{
        //    _settings = OASISSettings.Value;
        //}

        public SocialController()
        {
            //_settings = OASISSettings.Value;
        }

        /// <summary>
        /// Get's the social feed from all registered social providers for the currently logged in avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("share-holon")]
        public OASISResult<bool> GetSocialFeed()
        {
            // TODO: Finish implementing.
            return new();
        }

        /// <summary>
        /// Register a given social provider (FaceBook, Twitter, Instagram, LinkedIn, etc). PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("register-social-provider")]
        public OASISResult<bool> RegisterSocialProvider()
        {
            // TODO: Finish implementing.
            return new();
        }
    }
}
