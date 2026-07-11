using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// StarnetContextManager standalone endpoint — assembles a rich avatar context block from Web4 + Web5.
    /// </summary>
    [ApiController]
    [Route("v1/context")]
    public class ContextController : Web6ControllerBase
    {
        /// <summary>
        /// Returns a rich context block for an OASIS avatar: karma, active quests, missions, world memberships.
        /// Assembled from Web4 and Web5 in parallel. Useful for grounding AI prompts in real OASIS state.
        /// GET https://api.web6.oasisomniverse.one/v1/context/avatar/{avatarId}
        /// </summary>
        [HttpGet("avatar/{avatarId:guid}")]
        [ProducesResponseType(typeof(AvatarContextResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvatarContext(Guid avatarId)
        {
            string bearer = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            StarnetContextManager manager = new StarnetContextManager(avatarId, OASISDNA);
            var result = await manager.GetAvatarContextAsync(avatarId, bearer);
            return result.IsError ? BadRequest(result) : Ok(result);
        }
    }
}
