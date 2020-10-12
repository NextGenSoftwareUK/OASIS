using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/mission")]
    [ApiController]

    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    [EnableCors()]
    public class MissionController : OASISControllerBase
    {
        private MissionManager _missionManager;

        private MissionManager MissionManager
        {
            get
            {
                if (_missionManager == null)
                    _missionManager = new MissionManager(GetAndActivateProvider());

                return _missionManager;
            }
        }

        public MissionController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {

        }


        [HttpGet("Search/{searchParams}")]
        public ActionResult<ISearchResults> Search(ISearchParams searchParams)
        {
            return Ok(MissionManager.SearchAsync(searchParams).Result);
        }

        [HttpGet("Search/{searchParams}/{providerType}/{setGlobally}")]
        public ActionResult<ISearchResults> Search(ISearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            return Ok(MissionManager.SearchAsync(searchParams).Result);
        }

        [HttpPost("CompleteMission/{mission}")]
        public ActionResult<bool> CompleteMission(Mission mission)
        {
            return MissionManager.CompleteMission(mission);
        }

        [HttpPost("CreateMission/{mission}")]
        public ActionResult<bool> CreateMission(Mission mission)
        {
            return MissionManager.CreateMission(mission);
        }

        //TODO: GET WORKING LATER!
        //[HttpGet("GetAllCurrentMissionsForAvatar/{avatar}")]
        //public ActionResult<IMissionData> GetAllCurrentMissionsForAvatar(Avatar avatar)
        //{
        //    return Ok(MissionManager.GetAllCurrentMissionsForAvatar((IAvatar)avatar));
        //}

        //[HttpGet("GetAllCurrentMissionsForLoggedInAvatar")]
        //public ActionResult<List<Mission>> GetAllCurrentMissionsForLoggedInAvatar()
        //{
        //    return MissionManager.GetAllCurrentMissionsForAvatar(Avatar);
        //}

        [HttpPost("UpdateMission/{mission}")]
        public ActionResult<bool> UpdateMission(Mission mission)
        {
            return Ok(MissionManager.UpdateMission(mission));
        }

        [HttpDelete("DeleteMission/{mission}")]
        public ActionResult<bool> DeleteMission(Mission mission)
        {
            return MissionManager.DeleteMission(mission);
        }
    }
}
