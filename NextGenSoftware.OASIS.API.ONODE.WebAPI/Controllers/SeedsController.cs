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
        /// Pay someone with seeds.
        /// </summary>
        /// <param name="fromTelosAccountName"></param>
        /// <param name="toTelosAccountName"></param>
        /// <param name="qty"></param>
        /// <param name="memo"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("PayWithSeeds")]
        //public ActionResult<string> PayWithSeeds(string fromTelosAccountName, string toTelosAccountName, float qty, string memo)
        public ActionResult<string> PayWithSeeds(PayWithSeedsRequest request)
        {
            //return Ok(SEEDSManager.PayWithSeeds(fromTelosAccountName, toTelosAccountName, qty, memo));
            return Ok(SEEDSManager.PayWithSeeds(request.FromTelosAccountName, request.ToTelosAccountName, request.Qty, request.Memo));
        }

        /// <summary>
        /// Pay an avatar with seeds and receive karma and the PayWithSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="fromAvatarId"></param>
        /// <param name="toAvatarId"></param>
        /// <param name="qty"></param>
        /// <param name="memo"></param>
        /// <param name="receivingKarmaFor"></param>
        /// <param name="appWebsiteServiceName"></param>
        /// <param name="appWebsiteServiceDesc"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("PayAvatarWithSeeds")]
        public ActionResult<string> PayAvatarWithSeeds(Guid fromAvatarId, Guid toAvatarId, float qty, string memo, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            return Ok(SEEDSManager.PayWithSeeds(fromAvatarId, toAvatarId, qty, memo, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc));
        }

        /// <summary>
        /// Reward someone with seeds.
        /// </summary>
        /// <param name="fromTelosAccountName"></param>
        /// <param name="toTelosAccountName"></param>
        /// <param name="qty"></param>
        /// <param name="memo"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RewardWithSeeds")]
        public ActionResult<string> RewardWithSeeds(string fromTelosAccountName, string toTelosAccountName, float qty, string memo)
        {
            return Ok(SEEDSManager.RewardWithSeeds(fromTelosAccountName, toTelosAccountName, qty, memo));
        }

        /// <summary>
        /// Reward an avatar with seeds and receive karma and the RewardWithSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="fromAvatarId"></param>
        /// <param name="toAvatarId"></param>
        /// <param name="qty"></param>
        /// <param name="memo"></param>
        /// <param name="receivingKarmaFor"></param>
        /// <param name="appWebsiteServiceName"></param>
        /// <param name="appWebsiteServiceDesc"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RewardAvatarWithSeeds")]
        public ActionResult<string> RewardAvatarWithSeeds(Guid fromAvatarId, Guid toAvatarId, float qty, string memo, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            return Ok(SEEDSManager.RewardWithSeeds(fromAvatarId, toAvatarId, qty, memo, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc));
        }

        /// <summary>
        /// Donate someone with seeds.
        /// </summary>
        /// <param name="fromTelosAccountName"></param>
        /// <param name="toTelosAccountName"></param>
        /// <param name="qty"></param>
        /// <param name="memo"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DonateWithSeeds")]
        public ActionResult<string> DonateWithSeeds(string fromTelosAccountName, string toTelosAccountName, float qty, string memo)
        {
            return Ok(SEEDSManager.DonateWithSeeds(fromTelosAccountName, toTelosAccountName, qty, memo));
        }

        /// <summary>
        /// Donate an avatar with seeds and receive karma and the DonateWithSeeds &amp; SuperHero gifts.
        /// </summary>
        /// <param name="fromAvatarId"></param>
        /// <param name="toAvatarId"></param>
        /// <param name="qty"></param>
        /// <param name="memo"></param>
        /// <param name="receivingKarmaFor"></param>
        /// <param name="appWebsiteServiceName"></param>
        /// <param name="appWebsiteServiceDesc"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DonateAvatarWithSeeds")]
        public ActionResult<string> DonateAvatarWithSeeds(Guid fromAvatarId, Guid toAvatarId, float qty, string memo, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            return Ok(SEEDSManager.DonateWithSeeds(fromAvatarId, toAvatarId, qty, memo, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc));
        }

        /// <summary>
        /// Send an invite to someone to join Seeds.
        /// </summary>
        /// <param name="sponsorTelosAccountName"></param>
        /// <param name="referrerTelosAccountName"></param>
        /// <param name="transferQuantitiy"></param>
        /// <param name="sowQuantitiy"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SendInviteToJoinSeeds")]
        public ActionResult<string> SendInviteToJoinSeeds(string sponsorTelosAccountName, string referrerTelosAccountName, float transferQuantitiy, float sowQuantitiy)
        {
            return Ok(SEEDSManager.SendInviteToJoinSeeds(sponsorTelosAccountName, referrerTelosAccountName, transferQuantitiy, sowQuantitiy));
        }

        /// <summary>
        /// Send an invite to someone to join Seeds.
        /// </summary>
        /// <param name="sponsorAvatarId"></param>
        /// <param name="referrerAvatarId"></param>
        /// <param name="transferQuantitiy"></param>
        /// <param name="sowQuantitiy"></param>
        /// <param name="receivingKarmaFor"></param>
        /// <param name="appWebsiteServiceName"></param>
        /// <param name="appWebsiteServiceDesc"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SendInviteToAvatarJoinSeeds")]
        public ActionResult<string> SendInviteToAvatarJoinSeeds(Guid sponsorAvatarId, Guid referrerAvatarId, float transferQuantitiy, float sowQuantitiy, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            return Ok(SEEDSManager.SendInviteToJoinSeeds(sponsorAvatarId, referrerAvatarId, transferQuantitiy, sowQuantitiy, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc));
        }

        /// <summary>
        /// Accept an invite to join Seeds.
        /// </summary>
        /// <param name="sponsorTelosAccountName"></param>
        /// <param name="referrerTelosAccountName"></param>
        /// <param name="transferQuantitiy"></param>
        /// <param name="sowQuantitiy"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AcceptInviteToJoinSeeds")]
        public ActionResult<string> AcceptInviteToJoinSeeds(string sponsorTelosAccountName, string referrerTelosAccountName, float transferQuantitiy, float sowQuantitiy)
        {
            return Ok(SEEDSManager.SendInviteToJoinSeeds(sponsorTelosAccountName, referrerTelosAccountName, transferQuantitiy, sowQuantitiy));
        }

        /// <summary>
        /// Accept an invite to join Seeds.
        /// </summary>
        /// <param name="sponsorAvatarId"></param>
        /// <param name="referrerAvatarId"></param>
        /// <param name="transferQuantitiy"></param>
        /// <param name="sowQuantitiy"></param>
        /// <param name="receivingKarmaFor"></param>
        /// <param name="appWebsiteServiceName"></param>
        /// <param name="appWebsiteServiceDesc"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AcceptInviteToJoinSeeds")]
        public ActionResult<string> AcceptInviteToJoinSeeds(Guid sponsorAvatarId, Guid referrerAvatarId, float transferQuantitiy, float sowQuantitiy, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            return Ok(SEEDSManager.SendInviteToJoinSeeds(sponsorAvatarId, referrerAvatarId, transferQuantitiy, sowQuantitiy, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc));
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

        /// <summary>
        /// Generates a QR Code for logging into your Seeds Passport.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <param name="seedsKarmaType"></param>
        /// <param name="receivingKarmaFor"></param>
        /// <param name="appWebsiteServiceName"></param>
        /// <param name="appWebsiteServiceDesc"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AddKarmaForSeeds")]
        public ActionResult<bool> AddKarmaForSeeds(Guid avatarId, KarmaTypePositive seedsKarmaType, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            return Ok(SEEDSManager.AddKarmaForSeeds(avatarId, seedsKarmaType, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc));
        }
    }
}
