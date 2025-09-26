//using System;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Cors;
//using Microsoft.AspNetCore.Mvc;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Managers;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{
//    [Route("api/mission")]
//    [ApiController]

//    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
//    [EnableCors()]
//    public class MissionController : OASISControllerBase
//    {
//        private MissionManager _missionManager;

//        private MissionManager MissionManager
//        {
//            get
//            {
//                if (_missionManager == null)
//                    _missionManager = new MissionManager(GetAndActivateDefaultStorageProvider());

//                return _missionManager;
//            }
//        }

//        public MissionController()
//        {

//        }

//        /// <summary>
//        /// Search all missions for the given search parameters.
//        /// </summary>
//        /// <param name="searchParams"></param>
//        /// <returns></returns>
//        [HttpGet("Search/{searchParams}")]
//        public async Task<OASISResult<ISearchResults>> Search(ISearchParams searchParams)
//        {
//            return new(await MissionManager.SearchAsync(searchParams));
//        }

//        /// <summary>
//        /// Search all missions for the given search parameters. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
//        /// </summary>
//        /// <param name="searchParams"></param>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpGet("Search/{searchParams}/{providerType}/{setGlobally}")]
//        public async Task<OASISResult<ISearchResults>> Search(ISearchParams searchParams, ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(await MissionManager.SearchAsync(searchParams));
//        }

//        /// <summary>
//        /// Set's the given mission as completed.
//        /// </summary>
//        /// <param name="missionId"></param>
//        /// <returns></returns>
//        [HttpPost("CompleteMission/{missionId}")]
//        public OASISResult<bool> CompleteMission(Guid missionId)
//        {
//            return new(MissionManager.CompleteMission(missionId));
//        }

//        /// <summary>
//        /// Set's the given mission as completed. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
//        /// </summary>
//        /// <param name="missionId"></param>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpPost("CompleteMission/{missionId}/{providerType}/{setGlobally}")]
//        public OASISResult<bool> CompleteMission(Guid missionId, ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(MissionManager.CompleteMission(missionId));
//        }

//        /// <summary>
//        /// Create's a new mission.
//        /// </summary>
//        /// <param name="mission"></param>
//        /// <returns></returns>
//        [HttpPost("CreateMission/{mission}")]
//        public OASISResult<bool> CreateMission(Mission mission)
//        {
//            return new(MissionManager.CreateMission(mission));
//        }

//        /// <summary>
//        /// Create's a new mission. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
//        /// </summary>
//        /// <param name="mission"></param>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpPost("CreateMission/{mission}/{providerType}/{setGlobally}")]
//        public OASISResult<bool> CreateMission(Mission mission, ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(MissionManager.CreateMission(mission));
//        }

//        //TODO: GET WORKING LATER!
//        //[HttpGet("GetAllCurrentMissionsForAvatar/{avatar}")]
//        //public ActionResult<IMissionData> GetAllCurrentMissionsForAvatar(Avatar avatar)
//        //{
//        //    return Ok(MissionManager.GetAllCurrentMissionsForAvatar((IAvatar)avatar));
//        //}

//        //[HttpGet("GetAllCurrentMissionsForLoggedInAvatar")]
//        //public ActionResult<List<Mission>> GetAllCurrentMissionsForLoggedInAvatar()
//        //{
//        //    return MissionManager.GetAllCurrentMissionsForAvatar(Avatar);
//        //}

//        /// <summary>
//        /// Update's a given mission.
//        /// </summary>
//        /// <param name="mission"></param>
//        /// <returns></returns>
//        [HttpPost("UpdateMission/{mission}")]
//        public OASISResult<bool> UpdateMission(Mission mission)
//        {
//            return new(MissionManager.UpdateMission(mission));
//        }

//        /// <summary>
//        /// Update's a given mission. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
//        /// </summary>
//        /// <param name="mission"></param>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpPost("UpdateMission/{mission}/{providerType}/{setGlobally}")]
//        public OASISResult<bool> UpdateMission(Mission mission, ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(MissionManager.UpdateMission(mission));
//        }

//        /// <summary>
//        /// Delete's a given mission.
//        /// </summary>
//        /// <param name="missionId"></param>
//        /// <returns></returns>
//        [HttpDelete("DeleteMission/{missionId}")]
//        public OASISResult<bool> DeleteMission(Guid missionId)
//        {
//            return new(MissionManager.DeleteMission(missionId));
//        }

//        /// <summary>
//        /// Delete's a given mission. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
//        /// </summary>
//        /// <param name="missionId"></param>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpDelete("DeleteMission/{missionId}/{providerType}/{setGlobally}")]
//        public OASISResult<bool> DeleteMission(Guid missionId, ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(MissionManager.DeleteMission(missionId));
//        }
//    }
//}
