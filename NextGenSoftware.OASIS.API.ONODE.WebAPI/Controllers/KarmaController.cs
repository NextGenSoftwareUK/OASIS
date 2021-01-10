using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/karma")]
    [ApiController]

    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    [EnableCors()]
    public class KarmaController : OASISControllerBase
    {
        //private MissionManager _missionManager;

        //private MissionManager MissionManager
        //{
        //    get
        //    {
        //        if (_missionManager == null)
        //            _missionManager = new MissionManager(GetAndActivateProvider());

        //        return _missionManager;
        //    }
        //}

        public KarmaController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {

        }

        ///// <summary>
        ///// Get all positive karma weightings.
        ///// </summary>
        ///// <param name="karmaType"></param>
        ///// <returns></returns>
        //[HttpGet("GetAllPositiveKarmaWeightings/{karmaType}")]
        //public ActionResult<bool> GetAllPositiveKarmaWeightings(KarmaTypePositive karmaType)
        //{
        //    return Ok();
        //}

        /// <summary>
        /// Get karma weighting for a given positive karma cateogey.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <returns></returns>
        [HttpGet("GetPositiveKarmaWeighting/{karmaType}")]
        public ActionResult<bool> GetPositiveKarmaWeighting(KarmaTypePositive karmaType)
        {
            return Ok();
        }

        /// <summary>
        /// Get karma weighting for a given positive karma cateogey.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpGet("GetPositiveKarmaWeighting/{karmaType}/{providerType}/{setGlobally}")]
        public ActionResult<bool> GetPositiveKarmaWeighting(KarmaTypePositive karmaType, ProviderType providerType, bool setGlobally = false)
        {
            return Ok();
        }

        ///// <summary>
        ///// Get all negative karma weightings.
        ///// </summary>
        ///// <param name="karmaType"></param>
        ///// <returns></returns>
        //[HttpGet("GetAllNegativeKarmaWeightings/{karmaType}")]
        //public ActionResult<bool> GetAllNegativeKarmaWeightings(KarmaTypeNegative karmaType)
        //{
        //    return Ok();
        //}

        /// <summary>
        /// Get karma weighting for a given negative karma cateogey.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <returns></returns>
        [HttpGet("GetNegativeKarmaWeighting/{karmaType}")]
        public ActionResult<bool> GetNegativeKarmaWeighting(KarmaTypeNegative karmaType)
        {
            return Ok();
        }

        /// <summary>
        /// Get karma weighting for a given negative karma cateogey.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpGet("GetNegativeKarmaWeighting/{karmaType}/{providerType}/{setGlobally}")]
        public ActionResult<bool> GetNegativeKarmaWeighting(KarmaTypeNegative karmaType, ProviderType providerType, bool setGlobally = false)
        {
            return Ok();
        }

        /// <summary>
        /// Get's the karma for a given avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpGet("GetKarmaForAvatar/{avatarId}")]
        public ActionResult<bool> GetKarmaForAvatar(Guid avatarId)
        {
            return Ok();
        }

        /// <summary>
        /// Get's the karma for a given avatar. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpGet("GetKarmaForAvatar/{avatarId}/{providerType}/{setGlobally}")]
        public ActionResult<bool> GetKarmaForAvatar(Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            return Ok();
        }

        /// <summary>
        /// Allows people to vote what they feel should be the weighting for a given positive karma category.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("VoteForPositiveKarmaWeighting/{karmaType}/{weighting}")]
        public ActionResult<bool> VoteForPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting)
        {
            return Ok();
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
        [HttpPost("VoteForPositiveKarmaWeighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public ActionResult<bool> VoteForPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }

        /// <summary>
        /// Allows people to vote what they feel should be the weighting for a given negative karma category.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("VoteForNegativeKarmaWeighting/{karmaType}/{weighting}")]
        public ActionResult<bool> VoteForNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting)
        {
            return Ok();
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
        [HttpPost("VoteForNegativeKarmaWeighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public ActionResult<bool> VoteForNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }

        /// <summary>
        /// Set's the weighting for a given positive karma category.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("SetPositiveKarmaWeighting/{karmaType}/{weighting}")]
        public ActionResult<bool> SetPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting)
        {
            return Ok();
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
        [HttpPost("SetPositiveKarmaWeighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public ActionResult<bool> SetPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }

        /// <summary>
        /// Set's the weighting for a given negative karma category.
        /// </summary>
        /// <param name="karmaType"></param>
        /// <param name="weighting"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("SetNegativeKarmaWeighting/{karmaType}/{weighting}")]
        public ActionResult<bool> SetNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting)
        {
            return Ok();
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
        [HttpPost("SetNegativeKarmaWeighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public ActionResult<bool> SetNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }

        /// <summary>
        /// Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was earnt. They must be logged in &amp; authenticated for this method to work. 
        /// </summary>
        /// <param name="avatar">The avatar to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AddKarmaToAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}")]
        public ActionResult<IAvatar> AddKarmaToAvatar(IAvatar avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return Ok(Program.AvatarManager.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc));
        }

        /// <summary>
        /// Add positive karma to the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType = The type of positive karma, karmaSourceType = Where the karma was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was earnt. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatar">The avatar to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AddKarmaToAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> AddKarmaToAvatar(IAvatar avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return AddKarmaToAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        /// <summary>
        /// Remove karma from the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost.
        /// </summary>
        /// <param name="avatar">The avatar to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}")]
        public ActionResult<IAvatar> RemoveKarmaFromAvatar(IAvatar avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return Ok(Program.AvatarManager.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc));
        }

        /// <summary>
        /// Remove karma from the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatar">The avatar to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> RemoveKarmaFromAvatar(IAvatar avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return RemoveKarmaFromAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }
    }
}
