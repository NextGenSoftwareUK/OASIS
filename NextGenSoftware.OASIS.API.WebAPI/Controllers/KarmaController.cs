using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NextGenSoftware.OASIS.API.Core;

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

        [Authorize]
        [HttpPost("VoteForPositiveKarmaWeighting/{karmaType}/{weighting}")]
        public ActionResult<bool> VoteForPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting)
        {
            return Ok();
        }

        [Authorize]
        [HttpPost("VoteForPositiveKarmaWeighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public ActionResult<bool> VoteForPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }

        [Authorize]
        [HttpPost("VoteForNegativeKarmaWeighting/{karmaType}/{weighting}")]
        public ActionResult<bool> VoteForNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting)
        {
            return Ok();
        }

        [Authorize]
        [HttpPost("VoteForNegativeKarmaWeighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public ActionResult<bool> VoteForNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }


        [Authorize(AvatarType.Wizard)]
        [HttpPost("SetPositiveKarmaWeighting/{karmaType}/{weighting}")]
        public ActionResult<bool> SetPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting)
        {
            return Ok();
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost("SetPositiveKarmaWeighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public ActionResult<bool> SetPositiveKarmaWeighting(KarmaTypePositive karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost("SetNegativeKarmaWeighting/{karmaType}/{weighting}")]
        public ActionResult<bool> SetNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting)
        {
            return Ok();
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost("SetNegativeKarmaWeighting/{karmaType}/{weighting}/{providerType}/{setGlobally}")]
        public ActionResult<bool> SetNegativeKarmaWeighting(KarmaTypeNegative karmaType, int weighting, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }

        [Authorize]
        [HttpPost("AddKarmaToAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}")]
        public ActionResult<IAvatar> AddKarmaToAvatar(IAvatar avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return Ok(Program.AvatarManager.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc));
        }

        [Authorize]
        [HttpPost("AddKarmaToAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> AddKarmaToAvatar(IAvatar avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return AddKarmaToAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}")]
        public ActionResult<IAvatar> RemoveKarmaFromAvatar(IAvatar avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return Ok(Program.AvatarManager.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc));
        }

        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> RemoveKarmaFromAvatar(IAvatar avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return RemoveKarmaFromAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }
    }
}
