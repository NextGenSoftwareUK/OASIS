using Microsoft.AspNetCore.Mvc;
using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/seeds")]
    public class SeedsController : OASISControllerBase
    {
        //OASISSettings _settings;

        //public SeedsController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        //{
        //    _settings = OASISSettings.Value;
        //}

        SEEDSManager _SEEDSManager = null;

        SEEDSManager SEEDSManager
        {
            get
            {
                if (_SEEDSManager == null)
                    _SEEDSManager = new SEEDSManager((EOSIOOASIS)OASISConfigManager.GetAndActivateProvider(ProviderType.EOSOASIS));

                return _SEEDSManager;
            }
        }

        public SeedsController()
        {
            //_settings = OASISSettings.Value;
        }


        /// <summary>
        /// Get's all of the SEEDS Organisations.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllOrganisations")]
        public ActionResult<string> GetAllOrganisations()
        {
            return Ok(SEEDSManager.GetAllOrganisationsAsJSON());
        }

        /// <summary>
        /// Pay someone with seeds using their telos account and receive karma and the PayWithSeeds &amp; Hero gifts
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("PayWithSeedsUsingTelosAccount")]
        public ActionResult<string> PayWithSeedsUsingTelosAccount(PayWithSeedsUsingTelosAccountRequest request)
        {
            return Ok(SEEDSManager.PayWithSeedsUsingTelosAccount(request.FromTelosAccountName, request.ToTelosAccountName, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo));
        }

        /// <summary>
        /// Pay someone with seeds using their avatar and receive karma and the PayWithSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("PayWithSeedsUsingAvatar")]
        public ActionResult<string> PayWithSeedsUsingAvatar(PayWithSeedsUsingAvatarRequest request)
        {
            return Ok(SEEDSManager.PayWithSeedsUsingAvatar(request.FromAvatarId, request.ToAvatarId, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo));
        }

        /// <summary>
        /// Reward someone with seeds using their telos account and receive karma and the RewardWithSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RewardWithSeedsUsingTelosAccount")]
        public ActionResult<string> RewardWithSeedsUsingTelosAccount(PayWithSeedsUsingTelosAccountRequest request)
        {
            return Ok(SEEDSManager.RewardWithSeedsUsingTelosAccount(request.FromTelosAccountName, request.ToTelosAccountName, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo));
        }

        /// <summary>
        /// Reward someone with seeds using their avatar and receive karma and the RewardWithSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RewardWithSeedsUsingAvatar")]
        public ActionResult<string> RewardWithSeedsUsingAvatar(PayWithSeedsUsingAvatarRequest request)
        {
            return Ok(SEEDSManager.PayWithSeedsUsingAvatar(request.FromAvatarId, request.ToAvatarId, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo));
        }

        /// <summary>
        /// Donate someone with seeds using their telos account and receive karma and the DonateWithSeeds &amp; SuperHero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DonateWithSeedsUsingTelosAccount")]
        public ActionResult<string> DonateWithSeedsUsingTelosAccount(PayWithSeedsUsingTelosAccountRequest request)
        {
            return Ok(SEEDSManager.RewardWithSeedsUsingTelosAccount(request.FromTelosAccountName, request.ToTelosAccountName, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo));
        }

        /// <summary>
        /// Donate someone with seeds using their avatar and receive karma and the DonateWithSeeds &amp; SuperHero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DonateWithSeedssUsingAvatar")]
        public ActionResult<string> DonateWithSeedssUsingAvatar(PayWithSeedsUsingAvatarRequest request)
        {
            return Ok(SEEDSManager.PayWithSeedsUsingAvatar(request.FromAvatarId, request.ToAvatarId, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo));
        }

        /// <summary>
        /// Send an invite to someone to join Seeds using their telos account and then receive karma and the RewardWithSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SendInviteToJoinSeedsUsingTelosAccount")]
        public ActionResult<string> SendInviteToJoinSeedsUsingTelosAccount(SendInviteToJoinSeedsUsingTelosAccountRequest request)
        {
            return Ok(SEEDSManager.SendInviteToJoinSeedsUsingTelosAccount(request.SponsorTelosAccountName, request.RefererTelosAccountName, request.TransferQuanitity, request.SowQuanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink));
        }

        /// <summary>
        /// Send an invite to someone to join Seeds using avatar and then receive karma and the SendInviteToJoinSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SendInviteToJoinSeedsUsingAvatar")]
        public ActionResult<string> SendInviteToJoinSeedsUsingAvatar(SendInviteToJoinSeedsUsingAvatarRequest request)
        {
            return Ok(SEEDSManager.SendInviteToJoinSeedsUsingAvatar(request.SponsorAvatarId, request.RefererAvatarId, request.TransferQuanitity, request.SowQuanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink));
        }

        /// <summary>
        /// Accept an invite to join Seeds using their telos account and then receive karma and the AcceptInviteToJoinSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AcceptInviteToJoinSeedsUsingTelosAccount")]
        public ActionResult<string> AcceptInviteToJoinSeedsUsingTelosAccount(AcceptInviteToJoinSeedsUsingTelosAccountRequest request)
        {
            return Ok(SEEDSManager.AcceptInviteToJoinSeedsUsingTelosAccount(request.TelosAccountName, request.InviteSecret, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink));
        }

        /// <summary>
        /// Accept an invite to join Seeds using their avatar and then receive karma and the AcceptInviteToJoinSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AcceptInviteToJoinSeedsUsingAvatar")]
        public ActionResult<string> AcceptInviteToJoinSeedsUsingAvatar(AcceptInviteToJoinSeedsUsingAvatarRequest request)
        {
            return Ok(SEEDSManager.AcceptInviteToJoinSeedsUsingAvatar(request.AvatarId, request.InviteSecret, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink));
        }

        /// <summary>
        /// Get's the Telos account name for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccountNameForAvatar")]
        public ActionResult<string> GetTelosAccountNameForAvatar(Guid avatarId)
        {
            return Ok(SEEDSManager.GetTelosAccountNameForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Telos account.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccount")]
        public ActionResult<Account> GetTelosAccount(string telosAccountName)
        {
            return Ok(SEEDSManager.GetTelosAccount(telosAccountName));
        }

        /// <summary>
        /// Get's the Telos account for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccountForAvatar")]
        public ActionResult<Account> GetTelosAccountForAvatar(Guid avatarId)
        {
            return Ok(SEEDSManager.GetTelosAccountForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Avatar id for the the given Telos account name.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarIdForTelosAccountName")]
        public ActionResult<string> GetAvatarIdForTelosAccountName(string telosAccountName)
        {
            return Ok(SEEDSManager.GetAvatarIdForTelosAccountName(telosAccountName));
        }

        /// <summary>
        /// Get's the Avatar for the the given Telos account name.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarForTelosAccountName")]
        public ActionResult<string> GetAvatarForTelosAccountName(string telosAccountName)
        {
            return Ok(SEEDSManager.GetAvatarForTelosAccountName(telosAccountName));
        }

        /// <summary>
        /// Get's the SEEDS balance for the given Telos account.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalance")]
        public ActionResult<string> GetBalance(string telosAccountName)
        {
            return Ok(SEEDSManager.GetBalance(telosAccountName));
        }

        /// <summary>
        /// Get's the SEEDS balance for the given avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalanceForAvatar")]
        public ActionResult<string> GetBalanceForAvatar(Guid avatarId)
        {
            return Ok(SEEDSManager.GetBalanceForAvatar(avatarId));
        }

        /// <summary>
        /// Generates a QR Code for logging into your Seeds Passport.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GenerateSeedsPassportSignInQRCode")]
        public ActionResult<string> GenerateSeedsPassportSignInQRCode(string telosAccountName)
        {
            return Ok(SEEDSManager.GenerateSignInQRCode(telosAccountName));
        }

        /// <summary>
        /// Generates a QR Code for logging into your Seeds Passport.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GenerateSeedsPassportSignInQRCodeForAvatar")]
        public ActionResult<string> GenerateSeedsPassportSignInQRCodeForAvatar(Guid avatarId)
        {
            return Ok(SEEDSManager.GenerateSignInQRCodeForAvatar(avatarId));
        }

        ///// <summary>
        ///// Generates a QR Code for logging into your Seeds Passport.
        ///// </summary>
        ///// <param name="avatarId"></param>
        ///// <param name="seedsKarmaType"></param>
        ///// <param name="receivingKarmaFor"></param>
        ///// <param name="appWebsiteServiceName"></param>
        ///// <param name="appWebsiteServiceDesc"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("AddKarmaForSeeds")]
        //public ActionResult<bool> AddKarmaForSeeds(Guid avatarId, KarmaTypePositive seedsKarmaType, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        //{
        //    return Ok(SEEDSManager.AddKarmaForSeeds(avatarId, seedsKarmaType, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc));
        //}
    }
}
