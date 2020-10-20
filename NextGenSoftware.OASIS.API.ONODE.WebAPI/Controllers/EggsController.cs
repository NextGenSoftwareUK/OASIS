using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/eggs")]
    public class EggsController : OASISControllerBase
    {
        OASISSettings _settings;

        public EggsController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Get's all eggs currently hidden in the OASIS... PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllEggs")]
        public ActionResult<bool> GetAllEggs()
        {
            // TODO: Finish implementing.
            return Ok();
        }

        /// <summary>
        /// Get's the current egg quests. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetCurrentEggQuests")]
        public ActionResult<bool> GetCurrentEggQuests()
        {
            // TODO: Finish implementing.
            return Ok();
        }

        /// <summary>
        /// Get's the current egg quests. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetCurrentEggQuestLeaderBoard")]
        public ActionResult<bool> GetCurrentEggQuestLeaderBoard()
        {
            // TODO: Finish implementing.
            return Ok();
        }
    }
}
