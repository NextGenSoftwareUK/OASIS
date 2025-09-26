//using System;
//using Microsoft.AspNetCore.Cors;
//using Microsoft.AspNetCore.Mvc;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{
//    [Route("api/quest")]
//    [ApiController]

//    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
//    [EnableCors()]
//    public class QuestController : OASISControllerBase
//    {
//        private QuestManager _questManager;

//        private QuestManager QuestManager
//        {
//            get
//            {
//                if (_questManager == null)
//                    _questManager = new QuestManager(GetAndActivateDefaultStorageProvider());

//                return _questManager;
//            }
//        }

//        public QuestController() : base()
//        {

//        }

//        /// <summary>
//        /// Search all quests for the given search parameters.
//        /// </summary>
//        /// <param name="searchParams"></param>
//        /// <returns></returns>
//        [HttpGet("Search/{searchParams}")]
//        public OASISResult<ISearchResults> Search(ISearchParams searchParams)
//        {
//            return new(QuestManager.SearchAsync(searchParams).Result);
//        }

//        /// <summary>
//        /// Search all quests for the given search parameters.
//        /// </summary>
//        /// <param name="searchParams"></param>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpGet("Search/{searchParams}/{providerType}/{setGlobally}")]
//        public OASISResult<ISearchResults> Search(ISearchParams searchParams, ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(QuestManager.SearchAsync(searchParams).Result);
//        }

//        /// <summary>
//        /// Find nearest quest on the map.
//        /// </summary>
//        /// <returns></returns>
//        [HttpGet("FindNearestQuestOnMap")]
//        public OASISResult<Quest> FindNearestQuestOnMap()
//        {
//            return new(QuestManager.FindNearestQuestOnMap());
//        }

//        /// <summary>
//        /// Find nearest quest on the map. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
//        /// </summary>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpGet("FindNearestQuestOnMap/{providerType}/{setGlobally}")]
//        public OASISResult<Quest> FindNearestQuestOnMap(ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(QuestManager.FindNearestQuestOnMap());
//        }

//        /// <summary>
//        /// Marks a given quest as completed.
//        /// </summary>
//        /// <param name="quest"></param>
//        /// <returns></returns>
//        [HttpPost("CompleteQuest/{quest}")]
//        public OASISResult<bool> CompleteQuest(Guid questId)
//        {
//            return new(QuestManager.CompleteQuest(questId));
//        }

//        /// <summary>
//        /// Marks a given quest as completed. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
//        /// </summary>
//        /// <param name="quest"></param>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpPost("CompleteQuest/{quest}/{providerType}/{setGlobally}")]
//        public OASISResult<bool> CompleteQuest(Guid questId, ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(QuestManager.CompleteQuest(questId));
//        }

//        /// <summary>
//        /// Create a quest.
//        /// </summary>
//        /// <param name="quest"></param>
//        /// <returns></returns>
//        [HttpPost("CreateQuest/{quest}")]
//        public OASISResult<bool> CreateQuest(Quest quest)
//        {
//            return new(QuestManager.CreateQuest(quest));
//        }

//        /// <summary>
//        /// Create a quest. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
//        /// </summary>
//        /// <param name="quest"></param>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpPost("CreateQuest/{quest}/{providerType}/{setGlobally}")]
//        public OASISResult<bool> CreateQuest(Quest quest, ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(QuestManager.CreateQuest(quest));
//        }

//        /// <summary>
//        /// Highlight a given quest on the map.
//        /// </summary>
//        /// <param name="quest"></param>
//        /// <returns></returns>
//        [HttpPost("HighlightQuestOnMap/{quest}")]
//        public OASISResult<bool> HighlightQuestOnMap(Guid questId)
//        {
//            return new(QuestManager.HighlightQuestOnMap(questId));
//        }

//        /// <summary>
//        /// Highlight a given quest on the map. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
//        /// </summary>
//        /// <param name="quest"></param>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpPost("HighlightQuestOnMap/{quest}/{providerType}/{setGlobally}")]
//        public OASISResult<bool> HighlightQuestOnMap(Guid questId, ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(QuestManager.HighlightQuestOnMap(questId));
//        }



//        //[HttpGet("GetAllCurrentQuestsForAvatar/{avatar}")]
//        //public ActionResult<List<Quest>> GetAllCurrentquestsForAvatar(Avatar avatar)
//        //{
//        //    return questManager.GetAllCurrentQuestsForAvatar((IAvatar)avatar);
//        //}

//        //[HttpGet("GetAllCurrentQuestsForLoggedInAvatar")]
//        //public ActionResult<List<Quest>> GetAllCurrentQuestsForLoggedInAvatar()
//        //{
//        //    return questManager.GetAllCurrentQuestsForAvatar(Avatar);
//        //}

//        /// <summary>
//        /// Update a given quest.
//        /// </summary>
//        /// <param name="quest"></param>
//        /// <returns></returns>
//        [HttpPost("UpdateQuest/{quest}")]
//        public OASISResult<bool> UpdateQuest(Quest quest)
//        {
//            return new(QuestManager.UpdateQuest(quest));
//        }

//        /// <summary>
//        /// Update a given quest. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
//        /// </summary>
//        /// <param name="quest"></param>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpPost("UpdateQuest/{quest}/{providerType}/{setGlobally}")]
//        public OASISResult<bool> UpdateQuest(Quest quest, ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(QuestManager.UpdateQuest(quest));
//        }

//        /// <summary>
//        /// Delete a given quest.
//        /// </summary>
//        /// <param name="questId"></param>
//        /// <returns></returns>
//        [HttpDelete("DeleteQuest/{quest}")]
//        public OASISResult<bool> DeleteQuest(Guid questId)
//        {
//            return new(QuestManager.DeleteQuest(questId));
//        }

//        /// <summary>
//        /// Delete a given quest. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
//        /// </summary>
//        /// <param name="questId"></param>
//        /// <param name="providerType">Pass in the provider you wish to use.</param>
//        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
//        /// <returns></returns>
//        [HttpDelete("DeleteQuest/{quest}/{providerType}/{setGlobally}")]
//        public OASISResult<bool> DeleteQuest(Guid questId, ProviderType providerType, bool setGlobally = false)
//        {
//            GetAndActivateProvider(providerType, setGlobally);
//            return new(QuestManager.DeleteQuest(questId));
//        }
//    }
//}
