using System;
using Microsoft.AspNetCore.Mvc;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.Membranes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/seeds")]
    public class SeedsController : OASISControllerBase
    {
        SEEDSOASIS _SEEDSOASIS = null;

        SEEDSOASIS SEEDSOASIS
        {
            get
            {
                if (_SEEDSOASIS == null)
                {
                    /*
                    OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.TelosOASIS, OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.SEEDSOASIS.ConnectionString, true);

                    //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
                    if (result.IsError)
                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.TelosOASIS). Error details: ", result.Message), true, false, true);
                    */

                    // TODO: Currently SEEDSOASIS is injected in with a TelosOASIS provider with a SEEDS connectionstring and will unregister the TelosOASIS Provider first if it is already registered so it uses the SEEDS connection string instead.
                    // Not sure if we want SEEDSOASIS to use its own seperate private instance of the TelosOASIS provider using the SEEDS connection string allowing others to use the existing TelosOASIS Provider on the default Telos connectionstring?
                    // If that is the case then uncomment the bottom line and comment the top line.

                   // _SEEDSOASIS = new SEEDSOASIS((TelosOASIS)result.Result);
                    _SEEDSOASIS = new SEEDSOASIS(new TelosOASIS(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.SEEDSOASIS.ConnectionString));
                }

                return _SEEDSOASIS;
            }
        }

        public SeedsController()
        {

        }

        /// <summary>
        /// Get's all of the SEEDS Organisations.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllOrganisations")]
        public OASISResult<string> GetAllOrganisations()
        {
            return new(SEEDSOASIS.GetAllOrganisationsAsJSON());
        }

        /// <summary>
        /// Pay someone with seeds using their telos account and receive karma and the PayWithSeeds &amp; Hero gifts
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("PayWithSeedsUsingTelosAccount")]
        public OASISResult<string> PayWithSeedsUsingTelosAccount(PayWithSeedsUsingTelosAccountRequest request)
        {
            return SEEDSOASIS.PayWithSeedsUsingTelosAccount(request.FromTelosAccountName, request.FromTelosAccountPrivateKey, request.ToTelosAccountName, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo);
        }

        /// <summary>
        /// Pay someone with seeds using their avatar and receive karma and the PayWithSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("PayWithSeedsUsingAvatar")]
        public OASISResult<string> PayWithSeedsUsingAvatar(PayWithSeedsUsingAvatarRequest request)
        {
            return SEEDSOASIS.PayWithSeedsUsingAvatar(request.FromAvatarId, request.ToAvatarId, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo);
        }

        /// <summary>
        /// Reward someone with seeds using their telos account and receive karma and the RewardWithSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RewardWithSeedsUsingTelosAccount")]
        public OASISResult<string> RewardWithSeedsUsingTelosAccount(PayWithSeedsUsingTelosAccountRequest request)
        {
            return SEEDSOASIS.RewardWithSeedsUsingTelosAccount(request.FromTelosAccountName, request.FromTelosAccountPrivateKey, request.ToTelosAccountName, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo);
        }

        /// <summary>
        /// Reward someone with seeds using their avatar and receive karma and the RewardWithSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RewardWithSeedsUsingAvatar")]
        public OASISResult<string> RewardWithSeedsUsingAvatar(PayWithSeedsUsingAvatarRequest request)
        {
            return SEEDSOASIS.PayWithSeedsUsingAvatar(request.FromAvatarId, request.ToAvatarId, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo);
        }

        /// <summary>
        /// Donate someone with seeds using their telos account and receive karma and the DonateWithSeeds &amp; SuperHero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DonateWithSeedsUsingTelosAccount")]
        public OASISResult<string> DonateWithSeedsUsingTelosAccount(PayWithSeedsUsingTelosAccountRequest request)
        {
            return SEEDSOASIS.RewardWithSeedsUsingTelosAccount(request.FromTelosAccountName, request.FromTelosAccountPrivateKey, request.ToTelosAccountName, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo);
        }

        /// <summary>
        /// Donate someone with seeds using their avatar and receive karma and the DonateWithSeeds &amp; SuperHero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DonateWithSeedssUsingAvatar")]
        public OASISResult<string> DonateWithSeedssUsingAvatar(PayWithSeedsUsingAvatarRequest request)
        {
            return SEEDSOASIS.PayWithSeedsUsingAvatar(request.FromAvatarId, request.ToAvatarId, request.Quanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink, request.Memo);
        }

        /// <summary>
        /// Send an invite to someone to join Seeds using their telos account and then receive karma and the RewardWithSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SendInviteToJoinSeedsUsingTelosAccount")]
        public OASISResult<SendInviteResult> SendInviteToJoinSeedsUsingTelosAccount(SendInviteToJoinSeedsUsingTelosAccountRequest request)
        {
            return SEEDSOASIS.SendInviteToJoinSeedsUsingTelosAccount(request.SponsorTelosAccountName, request.SponsorTelosAccountPrivateKey, request.RefererTelosAccountName, request.TransferQuanitity, request.SowQuanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink);
        }

        /// <summary>
        /// Send an invite to someone to join Seeds using avatar and then receive karma and the SendInviteToJoinSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SendInviteToJoinSeedsUsingAvatar")]
        public OASISResult<SendInviteResult> SendInviteToJoinSeedsUsingAvatar(SendInviteToJoinSeedsUsingAvatarRequest request)
        {
            return SEEDSOASIS.SendInviteToJoinSeedsUsingAvatar(request.SponsorAvatarId, request.RefererAvatarId, request.TransferQuanitity, request.SowQuanitity, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink);
        }

        /// <summary>
        /// Accept an invite to join Seeds using their telos account and then receive karma and the AcceptInviteToJoinSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AcceptInviteToJoinSeedsUsingTelosAccount")]
        public OASISResult<string> AcceptInviteToJoinSeedsUsingTelosAccount(AcceptInviteToJoinSeedsUsingTelosAccountRequest request)
        {
            return SEEDSOASIS.AcceptInviteToJoinSeedsUsingTelosAccount(request.TelosAccountName, request.InviteSecret, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink);
        }

        /// <summary>
        /// Accept an invite to join Seeds using their avatar and then receive karma and the AcceptInviteToJoinSeeds &amp; Hero gifts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AcceptInviteToJoinSeedsUsingAvatar")]
        public OASISResult<string> AcceptInviteToJoinSeedsUsingAvatar(AcceptInviteToJoinSeedsUsingAvatarRequest request)
        {
            return SEEDSOASIS.AcceptInviteToJoinSeedsUsingAvatar(request.AvatarId, request.InviteSecret, request.ReceivingKarmaFor, request.AppWebsiteServiceName, request.AppWebsiteServiceDesc, request.AppWebsiteServiceWebLink);
        }

        /// <summary>
        /// Get's the Telos account name for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccountNameForAvatar")]
        public OASISResult<string> GetTelosAccountNameForAvatar(Guid avatarId)
        {
            return new(SEEDSOASIS.TelosOASIS.GetTelosAccountNameForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Telos account name for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccountPrivateKeyForAvatar")]
        public OASISResult<string> GetTelosAccountPrivateKeyForAvatar(Guid avatarId)
        {
            return new(SEEDSOASIS.TelosOASIS.GetTelosAccountPrivateKeyForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Telos account.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccount")]
        public OASISResult<Account> GetTelosAccount(string telosAccountName)
        {
            return new(SEEDSOASIS.TelosOASIS.GetTelosAccount(telosAccountName));
        }

        /// <summary>
        /// Get's the Telos account for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccountForAvatar")]
        public OASISResult<Account> GetTelosAccountForAvatar(Guid avatarId)
        {
            return new(SEEDSOASIS.TelosOASIS.GetTelosAccountForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Avatar id for the the given Telos account name.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarIdForTelosAccountName")]
        public OASISResult<string> GetAvatarIdForTelosAccountName(string telosAccountName)
        {
            return new(SEEDSOASIS.TelosOASIS.GetAvatarIdForTelosAccountName(telosAccountName).ToString());
        }

        /// <summary>
        /// Get's the Avatar for the the given Telos account name.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarForTelosAccountName")]
        public OASISResult<IAvatarDetail> GetAvatarForTelosAccountName(string telosAccountName)
        {
            return new (SEEDSOASIS.TelosOASIS.GetAvatarForTelosAccountName(telosAccountName));
        }

        /// <summary>
        /// Get's the SEEDS balance for the given Telos account.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalanceForTelosAccount")]
        public OASISResult<string> GetBalanceForTelosAccount(string telosAccountName)
        {
            return new(SEEDSOASIS.GetBalanceForTelosAccount(telosAccountName));
        }

        /// <summary>
        /// Get's the SEEDS balance for the given avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalanceForAvatar")]
        public OASISResult<string> GetBalanceForAvatar(Guid avatarId)
        {
            return new(SEEDSOASIS.GetBalanceForAvatar(avatarId));
        }

        /// <summary>
        /// Generates a QR Code for logging into your Seeds Passport.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GenerateSeedsPassportSignInQRCode")]
        public OASISResult<string> GenerateSeedsPassportSignInQRCode(string telosAccountName)
        {
            return new(SEEDSOASIS.GenerateSignInQRCode(telosAccountName));
        }

        /// <summary>
        /// Generates a QR Code for logging into your Seeds Passport.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GenerateSeedsPassportSignInQRCodeForAvatar")]
        public OASISResult<string> GenerateSeedsPassportSignInQRCodeForAvatar(Guid avatarId)
        {
            return new(SEEDSOASIS.GenerateSignInQRCodeForAvatar(avatarId));
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
        //    return Ok(SEEDSOASIS.AddKarmaForSeeds(avatarId, seedsKarmaType, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc));
        //}
    }
}
