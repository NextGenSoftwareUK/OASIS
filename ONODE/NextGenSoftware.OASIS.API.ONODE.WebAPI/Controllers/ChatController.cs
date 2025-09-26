using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : OASISControllerBase
    {
        //   OASISSettings _settings;

        //public ChatController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        public ChatController()
        {
            //_settings = OASISSettings.Value;
        }

        /// <summary>
        /// Starts a new chat session. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("start-new-chat-session")]
        public OASISResult<bool> StartNewChatSession()
        {
            // TODO: Finish implementing.
            return new ()
            {
                 IsError = false,
                 Result = true
            };
        }
    }
}
