using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Queues cross-game entity spawn events so one game can trigger entity spawns inside another.
    /// Events are stored per game+avatar queue; the target game polls for its next pending spawn.
    /// </summary>
    public record SpawnEvent(string avatarId, string game, string entityId, float x, float y, float z);

    /// <summary>Acknowledgement body for POST api/spawn-events/confirm.</summary>
    public record SpawnConfirmRequest(string avatarId, string entityId);

    [ApiController]
    [Route("api/spawn-events")]
    public class SpawnEventsController : ControllerBase
    {
        // Keyed by "{game}_{avatarId}" so each game/avatar pair has an independent event queue.
        private static readonly ConcurrentDictionary<string, Queue<SpawnEvent>> _events = new();
        private readonly ILogger<SpawnEventsController> _logger;

        public SpawnEventsController(ILogger<SpawnEventsController> logger)
        {
            _logger = logger;
        }

        private static string QueueKey(string game, string avatarId) => $"{game}_{avatarId}";

        /// <summary>
        /// Queues a cross-game entity spawn event.
        /// The target game retrieves it via GET api/spawn-events/pending.
        /// </summary>
        /// <param name="spawnEvent">Spawn details: avatarId, target game, entityId, and world coordinates.</param>
        /// <returns>200 OK on success.</returns>
        /// <response code="200">Spawn event queued successfully</response>
        /// <response code="400">Required fields are missing</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult QueueSpawnEvent([FromBody] SpawnEvent spawnEvent)
        {
            try
            {
                if (spawnEvent == null)
                    return BadRequest(new { error = "Request body is required." });

                if (string.IsNullOrWhiteSpace(spawnEvent.avatarId) ||
                    string.IsNullOrWhiteSpace(spawnEvent.game) ||
                    string.IsNullOrWhiteSpace(spawnEvent.entityId))
                    return BadRequest(new { error = "avatarId, game, and entityId are required." });

                var key = QueueKey(spawnEvent.game, spawnEvent.avatarId);
                var queue = _events.GetOrAdd(key, _ => new Queue<SpawnEvent>());
                lock (queue)
                {
                    queue.Enqueue(spawnEvent);
                }

                _logger.LogInformation(
                    "[SpawnEvents] Queued spawn event: game={Game} avatarId={AvatarId} entityId={EntityId} at ({X},{Y},{Z})",
                    spawnEvent.game, spawnEvent.avatarId, spawnEvent.entityId,
                    spawnEvent.x, spawnEvent.y, spawnEvent.z);

                return Ok(new { message = "Spawn event queued." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpawnEvents] Error queuing spawn event.");
                return StatusCode(500, new { error = "Internal server error queuing spawn event." });
            }
        }

        /// <summary>
        /// Pops and returns the next pending spawn event for the given game and avatar, or 204 if none.
        /// </summary>
        /// <param name="game">The game polling for spawn events (e.g. ODOOM, OQUAKE).</param>
        /// <param name="avatarId">The avatar whose events to retrieve.</param>
        /// <returns>200 with the next SpawnEvent, or 204 No Content if the queue is empty.</returns>
        /// <response code="200">Spawn event returned and removed from queue</response>
        /// <response code="204">No pending spawn events for this game/avatar</response>
        /// <response code="400">game or avatarId is missing</response>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(SpawnEvent), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetPendingSpawnEvent([FromQuery] string game, [FromQuery] string avatarId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(game) || string.IsNullOrWhiteSpace(avatarId))
                    return BadRequest(new { error = "game and avatarId query parameters are required." });

                var key = QueueKey(game, avatarId);
                if (_events.TryGetValue(key, out var queue))
                {
                    lock (queue)
                    {
                        if (queue.Count > 0)
                        {
                            var evt = queue.Dequeue();
                            _logger.LogInformation(
                                "[SpawnEvents] Dispatching spawn event for game={Game} avatarId={AvatarId} entityId={EntityId}.",
                                game, avatarId, evt.entityId);
                            return Ok(evt);
                        }
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpawnEvents] Error retrieving pending spawn event for game={Game} avatarId={AvatarId}.", game, avatarId);
                return StatusCode(500, new { error = "Internal server error retrieving spawn event." });
            }
        }

        /// <summary>
        /// Acknowledges a completed entity spawn. No persistent state is updated; this endpoint exists
        /// for logging and future confirmation tracking.
        /// </summary>
        /// <param name="request">avatarId and entityId of the spawned entity.</param>
        /// <returns>200 OK.</returns>
        /// <response code="200">Spawn confirmed</response>
        /// <response code="400">Required fields are missing</response>
        [HttpPost("confirm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult ConfirmSpawn([FromBody] SpawnConfirmRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Request body is required." });

                if (string.IsNullOrWhiteSpace(request.avatarId) || string.IsNullOrWhiteSpace(request.entityId))
                    return BadRequest(new { error = "avatarId and entityId are required." });

                _logger.LogInformation(
                    "[SpawnEvents] Spawn confirmed: avatarId={AvatarId} entityId={EntityId}.",
                    request.avatarId, request.entityId);

                return Ok(new { message = "Spawn confirmed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpawnEvents] Error confirming spawn.");
                return StatusCode(500, new { error = "Internal server error confirming spawn." });
            }
        }
    }
}
