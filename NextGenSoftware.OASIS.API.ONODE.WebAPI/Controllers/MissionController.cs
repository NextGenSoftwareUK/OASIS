using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NextGenSoftware.OASIS.API.Core;
using System;

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

        /// <summary>
        /// Search all missions for the given search parameters.
        /// </summary>
        /// <param name="searchParams"></param>
        /// <returns></returns>
        [HttpGet("Search/{searchParams}")]
        public ActionResult<ISearchResults> Search(ISearchParams searchParams)
        {
            return Ok(MissionManager.SearchAsync(searchParams).Result);
        }

        /// <summary>
        /// Search all missions for the given search parameters. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="searchParams"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpGet("Search/{searchParams}/{providerType}/{setGlobally}")]
        public ActionResult<ISearchResults> Search(ISearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok(MissionManager.SearchAsync(searchParams).Result);
        }

        /// <summary>
        /// Set's the given mission as completed.
        /// </summary>
        /// <param name="missionId"></param>
        /// <returns></returns>
        [HttpPost("CompleteMission/{missionId}")]
        public ActionResult<bool> CompleteMission(Guid missionId)
        {
            return MissionManager.CompleteMission(missionId);
        }

        /// <summary>
        /// Set's the given mission as completed. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="missionId"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpPost("CompleteMission/{missionId}/{providerType}/{setGlobally}")]
        public ActionResult<bool> CompleteMission(Guid missionId, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return MissionManager.CompleteMission(missionId);
        }

        /// <summary>
        /// Create's a new mission.
        /// </summary>
        /// <param name="mission"></param>
        /// <returns></returns>
        [HttpPost("CreateMission/{mission}")]
        public ActionResult<bool> CreateMission(Mission mission)
        {
            return MissionManager.CreateMission(mission);
        }

        /// <summary>
        /// Create's a new mission. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="mission"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpPost("CreateMission/{mission}/{providerType}/{setGlobally}")]
        public ActionResult<bool> CreateMission(Mission mission, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
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

        /// <summary>
        /// Update's a given mission.
        /// </summary>
        /// <param name="mission"></param>
        /// <returns></returns>
        [HttpPost("UpdateMission/{mission}")]
        public ActionResult<bool> UpdateMission(Mission mission)
        {
            return Ok(MissionManager.UpdateMission(mission));
        }

        /// <summary>
        /// Update's a given mission. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="mission"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpPost("UpdateMission/{mission}/{providerType}/{setGlobally}")]
        public ActionResult<bool> UpdateMission(Mission mission, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok(MissionManager.UpdateMission(mission));
        }

        /// <summary>
        /// Delete's a given mission.
        /// </summary>
        /// <param name="missionId"></param>
        /// <returns></returns>
        [HttpDelete("DeleteMission/{missionId}")]
        public ActionResult<bool> DeleteMission(Guid missionId)
        {
            return MissionManager.DeleteMission(missionId);
        }

        /// <summary>
        /// Delete's a given mission. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="missionId"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [HttpDelete("DeleteMission/{missionId}/{providerType}/{setGlobally}")]
        public ActionResult<bool> DeleteMission(Guid missionId, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return MissionManager.DeleteMission(missionId);
        }
    }
}
