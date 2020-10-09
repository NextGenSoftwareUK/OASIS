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

        [HttpGet("CompleteMission/{mission}")]
        public ActionResult<bool> CompleteMission(Mission mission)
        {
            return MissionManager.CompleteMission(mission);
        }

        [HttpGet("CreateMission/{mission}")]
        public ActionResult<bool> CreateMission(Mission mission)
        {
            return MissionManager.CreateMission(mission);
        }

        [HttpGet("DeleteMission/{mission}")]
        public ActionResult<bool> DeleteMission(Mission mission)
        {
            return MissionManager.DeleteMission(mission);
        }

        //[HttpGet("GetAllCurrentMissionsForAvatar/{avatar}")]
        //public ActionResult<List<Mission>> GetAllCurrentMissionsForAvatar(Avatar avatar)
        //{
        //    return MissionManager.GetAllCurrentMissionsForAvatar((IAvatar)avatar);
        //}

        //[HttpGet("GetAllCurrentMissionsForLoggedInAvatar")]
        //public ActionResult<List<Mission>> GetAllCurrentMissionsForLoggedInAvatar()
        //{
        //    return MissionManager.GetAllCurrentMissionsForAvatar(Avatar);
        //}

        //[HttpGet("SearchAsync/{searchParams}")]
        //public ActionResult<ISearchResults> SearchAsync(ISearchParams searchParams)
        //{
        //    return Ok(MissionManager.SearchAsync(searchParams).Result);
        //}

        //[HttpGet("UpdateMission/{mission}")]
        //public ActionResult<bool> UpdateMission(Mission mission)
        //{
        //    return Ok(MissionManager.UpdateMission(mission));
        //}
    }
}
