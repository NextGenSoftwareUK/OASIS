using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web7.Core.Managers;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.Web7.WebAPI.Controllers
{
    /// <summary>Shared intention fields where multiple consenting symbiosis sessions co-create. Only the aggregate state is ever exposed.</summary>
    [ApiController]
    [Route("v1/collective-consciousness")]
    public class CollectiveConsciousnessController : Web7ControllerBase
    {
        [HttpPost("spaces")]
        [ProducesResponseType(typeof(CollectiveConsciousnessSpace), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSpace([FromQuery] string name)
        {
            CollectiveConsciousnessManager manager = new CollectiveConsciousnessManager(AvatarId);
            var result = await manager.CreateSpaceAsync(name);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpPost("spaces/{spaceId}/join/{sessionId}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> JoinSpace(Guid spaceId, Guid sessionId)
        {
            CollectiveConsciousnessManager manager = new CollectiveConsciousnessManager(AvatarId);
            var result = await manager.JoinSpaceAsync(spaceId, sessionId);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpGet("spaces/{spaceId}/field")]
        [ProducesResponseType(typeof(CollectiveConsciousnessSpace), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAggregateField(Guid spaceId)
        {
            CollectiveConsciousnessManager manager = new CollectiveConsciousnessManager(AvatarId);
            var result = await manager.GetAggregateFieldAsync(spaceId);
            return result.IsError ? BadRequest(result) : Ok(result);
        }
    }
}
