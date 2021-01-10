using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/messaging")]
    public class MessagingController : OASISControllerBase
    {
        OASISSettings _settings;

        public MessagingController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Send's a message to the given avatar. PREVIEW - COMING SOON...
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SendMessageToAvatar/{avatar}")]
        public ActionResult<bool> SendMessageToAvatar(IAvatar avatar)
        {
            // TODO: Finish implementing.
            return Ok();
        }
    }
}
