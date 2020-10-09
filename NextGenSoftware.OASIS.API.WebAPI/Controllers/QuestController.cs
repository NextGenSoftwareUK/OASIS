//using Microsoft.AspNetCore.Cors;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;

//using NextGenSoftware.OASIS.API.Core;
//using System.Collections.Generic;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{
//    [Route("api/quest")]
//    [ApiController]

//    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
//    [EnableCors()]
//    public class QuestController : OASISControllerBase
//    {
//        private QuestManager _questManager;

//        private QuestManager questManager
//        {
//            get
//            {
//                if (_questManager == null)
//                    _questManager = new QuestManager(GetAndActivateProvider());

//                return _questManager;
//            }
//        }

//        public QuestController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
//        {

//        }

//        [HttpGet("CompleteQuest/{quest}")]
//        public ActionResult<bool> CompleteQuest(Quest quest)
//        {
//            return questManager.CompleteQuest(quest);
//        }

//        [HttpGet("CreateQuest/{quest}")]
//        public ActionResult<bool> CreateQuest(Quest quest)
//        {
//            return questManager.CreateQuest(quest);
//        }

//        [HttpGet("DeleteQuest/{quest}")]
//        public ActionResult<bool> DeleteQuest(Quest quest)
//        {
//            return questManager.DeleteQuest(quest);
//        }

//        [HttpGet("HighlightQuestOnMap/{quest}")]
//        public ActionResult<bool> HighlightQuestOnMap(Quest quest)
//        {
//            return questManager.HighlightQuestOnMap(quest);
//        }

//        [HttpGet("FindNearestQuestOnMap")]
//        public ActionResult<Quest> FindNearestQuestOnMap()
//        {
//            return questManager.FindNearestQuestOnMap();
//        }

//        [HttpGet("GetAllCurrentQuestsForAvatar/{avatar}")]
//        public ActionResult<List<Quest>> GetAllCurrentquestsForAvatar(Avatar avatar)
//        {
//            return questManager.GetAllCurrentQuestsForAvatar((IAvatar)avatar);
//        }

//        [HttpGet("GetAllCurrentQuestsForLoggedInAvatar")]
//        public ActionResult<List<Quest>> GetAllCurrentQuestsForLoggedInAvatar()
//        {
//            return questManager.GetAllCurrentQuestsForAvatar(Avatar);
//        }

//        [HttpGet("SearchAsync/{searchParams}")]
//        public ActionResult<ISearchResults> SearchAsync(ISearchParams searchParams)
//        {
//            return Ok(questManager.SearchAsync(searchParams).Result);
//        }

//        [HttpGet("UpdateQuest/{quest}")]
//        public ActionResult<bool> UpdateQuest(Quest quest)
//        {
//            return Ok(questManager.UpdateQuest(quest));
//        }
//    }
//}
