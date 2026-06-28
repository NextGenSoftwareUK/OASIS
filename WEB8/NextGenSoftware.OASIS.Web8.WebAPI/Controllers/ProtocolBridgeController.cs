using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web8.Core.Enums;
using NextGenSoftware.OASIS.Web8.Core.Managers;
using NextGenSoftware.OASIS.Web8.Core.Models;

namespace NextGenSoftware.OASIS.Web8.WebAPI.Controllers
{
    /// <summary>Translates any external system's wire format into the unified WEB8 MeshMessage envelope and back.</summary>
    [ApiController]
    [Route("v1/protocol-bridge")]
    public class ProtocolBridgeController : Web8ControllerBase
    {
        private static readonly ProtocolBridgeManager _bridge = new ProtocolBridgeManager();

        [HttpPost("translate-inbound")]
        [ProducesResponseType(typeof(MeshMessage), StatusCodes.Status200OK)]
        public IActionResult TranslateInbound([FromQuery] BridgeFormat format, [FromQuery] Guid sourceNodeId, [FromQuery] Guid destinationNodeId, [FromBody] string rawPayload)
        {
            MeshMessage message = _bridge.TranslateInbound(rawPayload, format, sourceNodeId, destinationNodeId);
            return Ok(message);
        }

        [HttpPost("translate-outbound")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public IActionResult TranslateOutbound([FromQuery] BridgeFormat targetFormat, [FromBody] MeshMessage message)
        {
            string translated = _bridge.TranslateOutbound(message, targetFormat);
            return Ok(translated);
        }
    }
}
