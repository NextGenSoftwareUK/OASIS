using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web7.Core.Enums;
using NextGenSoftware.OASIS.Web7.Core.Managers;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.Web7.WebAPI.Controllers
{
    /// <summary>The WEB7 symbiosis session lifecycle - start a consenting session, submit bio-signal batches, end the session.</summary>
    [ApiController]
    [Route("v1/symbiosis")]
    public class SymbiosisController : Web7ControllerBase
    {
        /// <summary>Starts a new symbiosis session. ConsentGranted must be explicitly true.</summary>
        [HttpPost("sessions")]
        [ProducesResponseType(typeof(SymbiosisSession), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartSession([FromQuery] bool consentGranted, [FromQuery] RetentionMode retention = RetentionMode.Ephemeral)
        {
            SymbiosisSessionManager manager = new SymbiosisSessionManager(AvatarId);
            var result = await manager.StartSessionAsync(AvatarId, consentGranted, retention);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpGet("sessions/{sessionId}")]
        [ProducesResponseType(typeof(SymbiosisSession), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSession(Guid sessionId)
        {
            SymbiosisSessionManager manager = new SymbiosisSessionManager(AvatarId);
            var result = await manager.GetSessionAsync(sessionId);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        /// <summary>Submits a batch of raw bio-signal samples and returns the freshly computed intention state.</summary>
        [HttpPost("sessions/{sessionId}/signals")]
        [ProducesResponseType(typeof(IntentionState), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitSignals(Guid sessionId, [FromBody] List<BioSignalSample> samples)
        {
            SymbiosisSessionManager manager = new SymbiosisSessionManager(AvatarId);
            var result = await manager.SubmitSignalsAsync(sessionId, samples);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        /// <summary>Ends the session instantly - with Ephemeral retention (the default), all signal-derived data is wiped immediately.</summary>
        [HttpPost("sessions/{sessionId}/end")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> EndSession(Guid sessionId)
        {
            SymbiosisSessionManager manager = new SymbiosisSessionManager(AvatarId);
            var result = await manager.EndSessionAsync(sessionId);
            return result.IsError ? BadRequest(result) : Ok(result);
        }
    }
}
