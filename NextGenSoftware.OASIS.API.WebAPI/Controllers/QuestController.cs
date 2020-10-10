using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/quest")]
    [ApiController]

    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    [EnableCors()]
    public class QuestController : OASISControllerBase
    {
        private QuestManager _questManager;

        private QuestManager QuestManager
        {
            get
            {
                if (_questManager == null)
                    _questManager = new QuestManager(GetAndActivateProvider());

                return _questManager;
            }
        }

        public QuestController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {

        }

        [HttpGet("Search/{searchParams}")]
        public ActionResult<ISearchResults> Search(ISearchParams searchParams)
        {
            return Ok(QuestManager.SearchAsync(searchParams).Result);
        }

        [HttpGet("Search/{searchParams}/{providerType}/{setGlobally}")]
        public ActionResult<ISearchResults> Search(ISearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            return Ok(QuestManager.SearchAsync(searchParams).Result);
        }

        [HttpGet("FindNearestQuestOnMap")]
        public ActionResult<Quest> FindNearestQuestOnMap()
        {
            return QuestManager.FindNearestQuestOnMap();
        }

        [HttpPost("CompleteQuest/{quest}")]
        public ActionResult<bool> CompleteQuest(Quest quest)
        {
            return QuestManager.CompleteQuest(quest);
        }

        [HttpPost("CreateQuest/{quest}")]
        public ActionResult<bool> CreateQuest(Quest quest)
        {
            return QuestManager.CreateQuest(quest);
        }

        [HttpPost("HighlightQuestOnMap/{quest}")]
        public ActionResult<bool> HighlightQuestOnMap(Quest quest)
        {
            return QuestManager.HighlightQuestOnMap(quest);
        }

       

        //[HttpGet("GetAllCurrentQuestsForAvatar/{avatar}")]
        //public ActionResult<List<Quest>> GetAllCurrentquestsForAvatar(Avatar avatar)
        //{
        //    return questManager.GetAllCurrentQuestsForAvatar((IAvatar)avatar);
        //}

        //[HttpGet("GetAllCurrentQuestsForLoggedInAvatar")]
        //public ActionResult<List<Quest>> GetAllCurrentQuestsForLoggedInAvatar()
        //{
        //    return questManager.GetAllCurrentQuestsForAvatar(Avatar);
        //}

        [HttpPost("UpdateQuest/{quest}")]
        public ActionResult<bool> UpdateQuest(Quest quest)
        {
            return Ok(QuestManager.UpdateQuest(quest));
        }

        [HttpDelete("DeleteQuest/{quest}")]
        public ActionResult<bool> DeleteQuest(Quest quest)
        {
            return QuestManager.DeleteQuest(quest);
        }
    }
}
