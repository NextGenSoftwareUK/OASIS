using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Registry for OASIS Omniverse portal endpoints.  Games register their portals on load
    /// and poll for unlock events.  When a quest triggers UnlockPortal the game calls
    /// ogengine_notify_portal_unlock which writes the IPC file; OGEditor/OmniverseKernel
    /// picks that up and posts here so other games can react.
    /// </summary>
    public record PortalRegistration(
        string portalId,
        string gameSource,
        string mapName,
        float x,
        float y,
        float z,
        string targetGame,
        string targetMap,
        string registeredBy
    );

    public record PortalRecord(
        string portalId,
        string gameSource,
        string mapName,
        float x,
        float y,
        float z,
        string targetGame,
        string targetMap,
        string registeredBy,
        bool isUnlocked,
        string? unlockedBy,
        DateTimeOffset? unlockedAt
    );

    public record PortalUnlockRequest(string avatarId);

    [ApiController]
    [Route("api/portals")]
    public class PortalsController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, PortalRecord> _portals = new();
        private readonly ILogger<PortalsController> _logger;

        public PortalsController(ILogger<PortalsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Register or update a portal endpoint.  Games call this on map load so the registry
        /// always knows which portals exist across the omniverse.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult RegisterPortal([FromBody] PortalRegistration reg)
        {
            if (reg is null || string.IsNullOrWhiteSpace(reg.portalId))
                return BadRequest(new { error = "portalId is required." });
            if (string.IsNullOrWhiteSpace(reg.gameSource))
                return BadRequest(new { error = "gameSource is required." });

            _portals.AddOrUpdate(
                reg.portalId,
                _ => new PortalRecord(reg.portalId, reg.gameSource, reg.mapName,
                                      reg.x, reg.y, reg.z, reg.targetGame, reg.targetMap,
                                      reg.registeredBy, false, null, null),
                (_, existing) => existing with
                {
                    gameSource = reg.gameSource,
                    mapName = reg.mapName,
                    x = reg.x, y = reg.y, z = reg.z,
                    targetGame = reg.targetGame,
                    targetMap = reg.targetMap,
                    registeredBy = reg.registeredBy
                });

            _logger.LogInformation("[Portals] Registered portal {PortalId} for game {Game} map {Map}",
                reg.portalId, reg.gameSource, reg.mapName);
            return Ok(new { message = "Portal registered.", portalId = reg.portalId });
        }

        /// <summary>
        /// List all registered portals (both locked and unlocked).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PortalRecord>), StatusCodes.Status200OK)]
        public IActionResult GetAllPortals()
        {
            return Ok(_portals.Values.ToList());
        }

        /// <summary>
        /// Get a specific portal by ID.
        /// </summary>
        [HttpGet("{portalId}")]
        [ProducesResponseType(typeof(PortalRecord), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetPortal(string portalId)
        {
            if (_portals.TryGetValue(portalId, out var portal))
                return Ok(portal);
            return NotFound(new { error = $"Portal '{portalId}' not found." });
        }

        /// <summary>
        /// Mark a portal as unlocked.  Called by OGEditor/OmniverseKernel when it detects
        /// the oasis_portal_unlock_{portalId}.json IPC file written by ogengine_notify_portal_unlock.
        /// </summary>
        [HttpPost("{portalId}/unlock")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult UnlockPortal(string portalId, [FromBody] PortalUnlockRequest req)
        {
            if (string.IsNullOrWhiteSpace(portalId))
                return BadRequest(new { error = "portalId is required." });

            if (!_portals.TryGetValue(portalId, out var existing))
            {
                /* Portal not yet registered — auto-register as a placeholder so the unlock is recorded. */
                existing = new PortalRecord(portalId, "unknown", string.Empty,
                                            0, 0, 0, string.Empty, string.Empty,
                                            req?.avatarId ?? string.Empty,
                                            false, null, null);
            }

            var unlocked = existing with
            {
                isUnlocked = true,
                unlockedBy = req?.avatarId,
                unlockedAt = DateTimeOffset.UtcNow
            };
            _portals[portalId] = unlocked;

            _logger.LogInformation("[Portals] Portal {PortalId} unlocked by avatar {AvatarId}",
                portalId, req?.avatarId ?? "(unknown)");
            return Ok(new { message = "Portal unlocked.", portalId, unlockedAt = unlocked.unlockedAt });
        }

        /// <summary>
        /// Reset (re-lock) a portal.  Use for testing or when a quest resets.
        /// </summary>
        [HttpPost("{portalId}/lock")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult LockPortal(string portalId)
        {
            if (!_portals.TryGetValue(portalId, out var existing))
                return NotFound(new { error = $"Portal '{portalId}' not found." });

            _portals[portalId] = existing with { isUnlocked = false, unlockedBy = null, unlockedAt = null };
            _logger.LogInformation("[Portals] Portal {PortalId} re-locked.", portalId);
            return Ok(new { message = "Portal locked.", portalId });
        }
    }
}
