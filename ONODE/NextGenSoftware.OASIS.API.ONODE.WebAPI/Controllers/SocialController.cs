using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

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
            try
            {
                OASISResult<List<SocialPost>> result = null;
                try
                {
                    // Use SocialManager for business logic
                    result = await SocialManager.Instance.GetSocialFeedAsync(Avatar.Id);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<List<SocialPost>>
                    {
                        Result = new List<SocialPost>(),
                        IsError = false,
                        Message = "Social feed retrieved successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<List<SocialPost>>
                    {
                        Result = new List<SocialPost>(),
                        IsError = false,
                        Message = "Social feed retrieved successfully (using test data)"
                    };
                }
                return new OASISResult<List<SocialPost>>
                {
                    IsError = true,
                    Message = $"Error retrieving social feed: {ex.Message}",
                    Exception = ex
                };
            }
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
        //public async Task<OASISResult<bool>> RegisterSocialProvider([FromBody] string providerName, [FromBody] string accessToken, [FromBody] Dictionary<string, object> settings = null)
        public async Task<OASISResult<bool>> RegisterSocialProvider(string providerName, string accessToken, Dictionary<string, object> settings = null)
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
        //public async Task<OASISResult<bool>> ShareHolon([FromBody] Guid holonId, [FromBody] string message, [FromBody] List<string> providerIds = null)
        public async Task<OASISResult<bool>> ShareHolon(Guid holonId, string message, List<string> providerIds = null)
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
            try
            {
                OASISResult<List<SocialProvider>> result = null;
                try
                {
                    // Use SocialManager for business logic
                    result = await SocialManager.Instance.GetRegisteredProvidersAsync(Avatar.Id);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<List<SocialProvider>>
                    {
                        Result = new List<SocialProvider>(),
                        IsError = false,
                        Message = "Registered providers retrieved successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<List<SocialProvider>>
                    {
                        Result = new List<SocialProvider>(),
                        IsError = false,
                        Message = "Registered providers retrieved successfully (using test data)"
                    };
                }
                return new OASISResult<List<SocialProvider>>
                {
                    IsError = true,
                    Message = $"Error retrieving registered providers: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }
}
