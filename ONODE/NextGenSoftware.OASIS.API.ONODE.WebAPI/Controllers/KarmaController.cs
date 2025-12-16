using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Karma management endpoints for tracking and managing user karma points, achievements, and spiritual growth.
    /// Supports karma weighting, positive/negative karma tracking, and spiritual development metrics.
    /// </summary>
    [Route("api/karma")]
    [ApiController]
    [EnableCors()]
    public class KarmaController : OASISControllerBase
    {
        /// <summary>
        /// Get karma weighting for a given positive karma category.
        /// </summary>
        /// <param name="karmaType">The positive karma type to get weighting for.</param>
        /// <returns>OASIS result containing the karma weighting value.</returns>
        /// <response code="200">Karma weighting retrieved successfully</response>
        /// <response code="400">Invalid karma type provided</response>
        [HttpGet("get-positive-karma-weighting/{karmaType}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public OASISResult<bool> GetPositiveKarmaWeighting(KarmaTypePositive karmaType)
        {
            return new();
        }

        /// <summary>
        /// Get karma weighting for a given positive karma category with specific provider.
        /// </summary>
        /// <param name="karmaType">The positive karma type to get weighting for.</param>
        /// <param name="providerType">The OASIS provider type to use for the operation.</param>
        /// <param name="setGlobally">Whether to set this provider globally for all future requests.</param>
        /// <returns>OASIS result containing the karma weighting value.</returns>
        /// <response code="200">Karma weighting retrieved successfully</response>
        /// <response code="400">Invalid karma type or provider type provided</response>
        [HttpGet("get-positive-karma-weighting/{karmaType}/{providerType}/{setGlobally}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public OASISResult<bool> GetPositiveKarmaWeighting(KarmaTypePositive karmaType, ProviderType providerType, bool setGlobally = false)
        {
            return new();
        }

        /// <summary>
        /// Get karma weighting for a given negative karma category.
        /// </summary>
        /// <param name="karmaType">The negative karma type to get weighting for.</param>
        /// <returns>OASIS result containing the karma weighting value.</returns>
        /// <response code="200">Karma weighting retrieved successfully</response>
        /// <response code="400">Invalid karma type provided</response>
        [HttpGet("get-negative-karma-weighting/{karmaType}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public OASISResult<bool> GetNegativeKarmaWeighting(KarmaTypeNegative karmaType)
        {
            return new();
        }

        /// <summary>
        /// Get karma weighting for a given negative karma cateogey.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpGet("get-negative-karma-weighting/{karmaType}/{providerType}/{setGlobally}")]
        public OASISResult<bool> GetNegativeKarmaWeighting(KarmaTypeNegative karmaType, ProviderType providerType, bool setGlobally = false)
        {
            return new();
        }

        /// <summary>
        /// Get's the karma for a given avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpGet("get-karma-for-avatar/{avatarId}")]
        public async Task<OASISResult<long>> GetKarmaForAvatar(Guid avatarId)
        {
            return await KarmaManager.Instance.GetKarmaAsync(avatarId);
        }

        /// <summary>
        /// Get's the karma for a given avatar. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpGet("get-karma-for-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<long>> GetKarmaForAvatar(Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetKarmaForAvatar(avatarId);
        }

        /// <summary>
        /// Get's the karma akashic records for a given avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [HttpGet("get-karma-akashic-records-for-avatar/{avatarId}")]
        public OASISResult<IEnumerable<IKarmaAkashicRecord>> GetKarmaAkashicRecordsForAvatar(Guid avatarId)
        {
            OASISResult<IAvatarDetail> avatarResult = Program.AvatarManager.LoadAvatarDetail(avatarId);

            if (!avatarResult.IsError && avatarResult.Result != null)
                return new OASISResult<IEnumerable<IKarmaAkashicRecord>>(avatarResult.Result.KarmaAkashicRecords);
            else
                return new OASISResult<IEnumerable<IKarmaAkashicRecord>>() { IsError = true, Message = $"Error loading avatar detail. Reason: {avatarResult.Message}" };
        }

        /// <summary>
        /// Get's the karma akashic records for a given avatar. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>+
        [HttpGet("get-karma-akashic-records-for-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public OASISResult<IEnumerable<IKarmaAkashicRecord>> GetKarmaAkashicRecordsForAvatar(Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return GetKarmaAkashicRecordsForAvatar(avatarId);
        }

        /// <summary>
        /// Allows people to vote what they feel should be the weighting for a given positive karma category.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("vote-for-positive-karma-weighting/{karmaType}/{weighting}")]
        public OASISResult<bool> VoteForPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting)
        {
            return new();
        }

        /// <summary>
        /// Allows people to vote what they feel should be the weighting for a given positive karma category. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("vote-for-positive-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public OASISResult<bool> VoteForPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return new();
        }

        /// <summary>
        /// Allows people to vote what they feel should be the weighting for a given negative karma category.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("vote-for-negative-karma-weighting/{karmaType}/{weighting}")]
        public OASISResult<bool> VoteForNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting)
        {
            return new();
        }

        /// <summary>
        /// Allows people to vote what they feel should be the weighting for a given negative karma category. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("vote-for-negative-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public OASISResult<bool> VoteForNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return new();
        }

        /// <summary>
        /// Set's the weighting for a given positive karma category.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("set-positive-karma-weighting/{karmaType}/{weighting}")]
        public OASISResult<bool> SetPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting)
        {
            return new();
        }

        /// <summary>
        /// Set's the weighting for a given positive karma category. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("set-positive-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public OASISResult<bool> SetPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return new();
        }

        /// <summary>
        /// Set's the weighting for a given negative karma category.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("set-negative-karma-weighting/{karmaType}/{weighting}")]
        public OASISResult<bool> SetNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting)
        {
            return new();
        }

        /// <summary>
        /// Set's the weighting for a given positive karma category. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("set-negative-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public OASISResult<bool> SetNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return new();
        }

        /// <summary>
        /// Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was earnt. They must be logged in &amp; authenticated for this method to work. 
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("add-karma-to-avatar/{avatarId}")]
        public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            object karmaTypePositiveObject = null;
            object karmaSourceTypeObject = null;

            if (!Enum.TryParse(typeof(KarmaTypePositive), addKarmaToAvatarRequest.KarmaType, out karmaTypePositiveObject))
                return new() { Message = string.Concat("ERROR: KarmaType needs to be one of the values found in KarmaTypePositive enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaTypePositive))), IsError = true };
            if (!Enum.TryParse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType, out karmaSourceTypeObject))
                return new() { Message = string.Concat("ERROR: KarmaSourceType needs to be one of the values found in KarmaSourceType enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaSourceType))), IsError = true };
            return await KarmaManager.Instance.AddKarmaToAvatarAsync(avatarId, (KarmaTypePositive)karmaTypePositiveObject, (KarmaSourceType)karmaSourceTypeObject, addKarmaToAvatarRequest.KaramSourceTitle, addKarmaToAvatarRequest.KarmaSourceDesc);
        }

        /// <summary>
        /// Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was earnt. They must be logged in &amp; authenticated for this method to work. 
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("add-karma-to-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await AddKarmaToAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        /// Remove karma from the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("remove-karma-from-avatar/{avatarId}")]
        public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            object karmaTypeNegativeObject = null;
            object karmaSourceTypeObject = null;

            if (!Enum.TryParse(typeof(KarmaTypeNegative), addKarmaToAvatarRequest.KarmaType, out karmaTypeNegativeObject))
                return new() { IsError = true, Message = string.Concat("ERROR: KarmaType needs to be one of the values found in KarmaTypeNegative enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaTypeNegative))) };
            if (!Enum.TryParse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType, out karmaSourceTypeObject))
                return new() { IsError = true, Message = string.Concat("ERROR: KarmaSourceType needs to be one of the values found in KarmaSourceType enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaSourceType))) };
            return await KarmaManager.Instance.RemoveKarmaFromAvatarAsync(avatarId, (KarmaTypeNegative)karmaTypeNegativeObject, (KarmaSourceType)karmaSourceTypeObject, addKarmaToAvatarRequest.KaramSourceTitle, addKarmaToAvatarRequest.KarmaSourceDesc);
        }

        /// <summary>
        /// Remove karma from the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("remove-karma-from-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await RemoveKarmaFromAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        /// Gets karma statistics (totals, distributions, recent activity) for a given avatar.
        /// </summary>
        [HttpGet("get-karma-stats/{avatarId}")]
        public async Task<OASISResult<Dictionary<string, object>>> GetKarmaStats(Guid avatarId)
        {
            return await KarmaManager.Instance.GetKarmaStatsAsync(avatarId);
        }

        /// <summary>
        /// Gets karma statistics for a given avatar with provider activation.
        /// </summary>
        [HttpGet("get-karma-stats/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<Dictionary<string, object>>> GetKarmaStats(Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetKarmaStats(avatarId);
        }

        /// <summary>
        /// Gets karma history (paged) for a given avatar.
        /// </summary>
        [HttpGet("get-karma-history/{avatarId}")]
        public async Task<OASISResult<List<KarmaTransaction>>> GetKarmaHistory(Guid avatarId, int limit = 50, int offset = 0)
        {
            return await KarmaManager.Instance.GetKarmaHistoryAsync(avatarId, limit, offset);
        }

        /// <summary>
        /// Gets karma history (paged) for a given avatar with provider activation.
        /// </summary>
        [HttpGet("get-karma-history/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<List<KarmaTransaction>>> GetKarmaHistory(Guid avatarId, ProviderType providerType, bool setGlobally = false, int limit = 50, int offset = 0)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetKarmaHistory(avatarId, limit, offset);
        }

        // NOTE: Karma cannot be transferred; endpoint intentionally commented out per product rules
        // [Authorize]
        // [HttpPost("transfer-karma")]
        // public async Task<OASISResult<bool>> TransferKarma([FromBody] TransferKarmaRequest request)
        // {
        //     return await KarmaManager.Instance.TransferKarmaAsync(request.FromAvatarId, request.ToAvatarId, request.Amount, request.Description);
        // }
    }
}
