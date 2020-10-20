using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : OASISControllerBase
    {
        OASISSettings _settings;

        public ChatController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Starts a new chat session. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("StartNewChatSession")]
        public ActionResult<bool> StartNewChatSession()
        {
            // TODO: Finish implementing.
            return Ok();
        }
    }
}
