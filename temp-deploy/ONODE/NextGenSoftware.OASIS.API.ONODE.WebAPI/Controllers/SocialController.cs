using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        /// Get's the social feed from all registered social providers for the currently logged in avatar
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("social-feed")]
        public async Task<OASISResult<List<SocialPost>>> GetSocialFeed()
        {
            // Use SocialManager for business logic
            return await SocialManager.Instance.GetSocialFeedAsync(Avatar.Id);
        }

        /// <summary>
        /// Register a given social provider (FaceBook, Twitter, Instagram, LinkedIn, etc)
        /// </summary>
        /// <param name="providerName">Name of the social provider</param>
        /// <param name="accessToken">Access token for the provider</param>
        /// <param name="settings">Additional provider settings</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("register-social-provider")]
        public async Task<OASISResult<bool>> RegisterSocialProvider([FromBody] string providerName, [FromBody] string accessToken, [FromBody] Dictionary<string, object> settings = null)
        {
            // Use SocialManager for business logic
            return await SocialManager.Instance.RegisterSocialProviderAsync(Avatar.Id, providerName, accessToken, settings);
        }

        /// <summary>
        /// Share a holon to social media
        /// </summary>
        /// <param name="holonId">ID of the holon to share</param>
        /// <param name="message">Message to accompany the share</param>
        /// <param name="providerIds">Specific provider IDs to share to (optional)</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("share-holon")]
        public async Task<OASISResult<bool>> ShareHolon([FromBody] Guid holonId, [FromBody] string message, [FromBody] List<string> providerIds = null)
        {
            // Use SocialManager for business logic
            return await SocialManager.Instance.ShareHolonAsync(Avatar.Id, holonId, message, providerIds);
        }

        /// <summary>
        /// Get registered social providers for the current avatar
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("registered-providers")]
        public async Task<OASISResult<List<SocialProvider>>> GetRegisteredProviders()
        {
            // Use SocialManager for business logic
            return await SocialManager.Instance.GetRegisteredProvidersAsync(Avatar.Id);
        }
    }
}
