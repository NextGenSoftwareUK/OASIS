using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/messaging")]
    public class MessagingController : OASISControllerBase
    {
        //OASISSettings _settings;

        //public MessagingController(IOptions<OASISDNA> OASISSettings) : base(OASISSettings)
        //{
        //    _settings = OASISSettings.Value;
        //}

        public MessagingController() 
        {

        }

        /// <summary>
        /// Send's a message to the given avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("send-message-to-avatar/{avatar}")]
        public OASISResult<bool> SendMessageToAvatar(IAvatar avatar)
        {
            // TODO: Finish implementing.
            return new();
        }
    }
}
