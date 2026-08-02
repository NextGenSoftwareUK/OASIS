using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Records and dispatches cross-game teleport requests so native games can hand off an avatar to another game world.
    /// Requests are stored in-memory and keyed by avatarId; the target game polls for its pending teleport on login/map-load.
    /// </summary>
    public record TeleportRequest(
        string sourceGame,
        string sourceMap,
        string targetGame,
        string targetMap,
        float spawnX,
        float spawnY,
        float spawnZ,
        string avatarId,
        string sessionToken
    );

    [ApiController]
    [Route("api/teleport")]
    public class TeleportController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, TeleportRequest> _pending = new();
        private readonly ILogger<TeleportController> _logger;

        public TeleportController(ILogger<TeleportController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Records a cross-game teleport request for the given avatar.
        /// The target game will pick this up via GET api/teleport/pending.
        /// </summary>
        /// <param name="request">Teleport details including source/target game, map, spawn coordinates, avatarId, and sessionToken.</param>
        /// <returns>200 OK on success.</returns>
        /// <response code="200">Teleport request recorded successfully</response>
        /// <response code="400">Request body is null or avatarId is missing</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult RecordTeleport([FromBody] TeleportRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Request body is required." });

                if (string.IsNullOrWhiteSpace(request.avatarId))
                    return BadRequest(new { error = "avatarId is required." });

                _pending[request.avatarId] = request;
                _logger.LogInformation(
                    "[Teleport] Queued teleport for avatar {AvatarId}: {SourceGame}/{SourceMap} -> {TargetGame}/{TargetMap}",
                    request.avatarId, request.sourceGame, request.sourceMap, request.targetGame, request.targetMap);

                return Ok(new { message = "Teleport request recorded." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Teleport] Error recording teleport request.");
                return StatusCode(500, new { error = "Internal server error recording teleport request." });
            }
        }

        /// <summary>
        /// Returns and removes the pending teleport request for the given avatarId, or 204 if none exists.
        /// Games should poll this on map load or at an interval to detect incoming cross-game teleports.
        /// </summary>
        /// <param name="avatarId">The avatar whose pending teleport to retrieve.</param>
        /// <returns>200 with the TeleportRequest body, or 204 No Content if no pending request.</returns>
        /// <response code="200">Pending teleport found and returned</response>
        /// <response code="204">No pending teleport for this avatar</response>
        /// <response code="400">avatarId query parameter is missing</response>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(TeleportRequest), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetPendingTeleport([FromQuery] string avatarId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(avatarId))
                    return BadRequest(new { error = "avatarId query parameter is required." });

                if (_pending.TryRemove(avatarId, out var request))
                {
                    _logger.LogInformation("[Teleport] Dispatching pending teleport for avatar {AvatarId}.", avatarId);
                    return Ok(request);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Teleport] Error retrieving pending teleport for avatar {AvatarId}.", avatarId);
                return StatusCode(500, new { error = "Internal server error retrieving teleport request." });
            }
        }
    }
}
