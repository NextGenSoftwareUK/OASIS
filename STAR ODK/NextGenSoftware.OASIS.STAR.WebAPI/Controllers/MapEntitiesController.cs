using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>An entity placed on a game map with a position.</summary>
    public record MapEntity(string entityId, float x, float y, float z);

    /// <summary>
    /// Manages per-map entity lists across OASIS games.
    /// Entity lists are loaded from *.json files in Config/map_entities/ (named {game}_{map}.json)
    /// on startup and kept in memory. PUT replaces the entity list for a map and persists it to disk.
    /// </summary>
    [ApiController]
    [Route("api/maps")]
    public class MapEntitiesController : ControllerBase
    {
        // Keyed by "{game}_{map}" (e.g. "ODOOM_E1M3")
        private static readonly Dictionary<string, List<MapEntity>> _entities = new();
        private static bool _loaded = false;
        private static readonly object _lock = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly ILogger<MapEntitiesController> _logger;
        private readonly IWebHostEnvironment _env;

        public MapEntitiesController(ILogger<MapEntitiesController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
            EnsureLoaded();
        }

        // ── Path helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the OASIS Omniverse Config/map_entities/ directory.
        /// Navigates two levels up from the WebAPI content root to reach the OASIS2 monorepo root,
        /// then descends into "OASIS Omniverse/Config/map_entities".
        /// </summary>
        private string MapEntitiesDir
        {
            get
            {
                var oasis2Root = Path.GetDirectoryName(Path.GetDirectoryName(_env.ContentRootPath));
                return Path.Combine(oasis2Root ?? AppContext.BaseDirectory, "OASIS Omniverse", "Config", "map_entities");
            }
        }

        private static string MapKey(string game, string map) => $"{game}_{map}";

        // ── Startup load ─────────────────────────────────────────────────────────

        private void EnsureLoaded()
        {
            if (_loaded) return;
            lock (_lock)
            {
                if (_loaded) return;
                LoadFromDisk();
                _loaded = true;
            }
        }

        private void LoadFromDisk()
        {
            var dir = MapEntitiesDir;
            if (!Directory.Exists(dir))
            {
                _logger.LogInformation("[MapEntities] Config/map_entities/ directory not found at {Dir}; starting with empty set.", dir);
                Directory.CreateDirectory(dir);
                return;
            }

            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var key = Path.GetFileNameWithoutExtension(file); // e.g. "ODOOM_E1M3"
                    var json = File.ReadAllText(file);
                    var list = JsonSerializer.Deserialize<List<MapEntity>>(json, _jsonOpts);
                    if (list != null)
                    {
                        _entities[key] = list;
                        _logger.LogInformation("[MapEntities] Loaded {Count} entities for map '{Key}' from {File}.", list.Count, key, file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[MapEntities] Failed to parse entity list from {File}; skipping.", file);
                }
            }

            _logger.LogInformation("[MapEntities] Loaded entity lists for {Count} map(s).", _entities.Count);
        }

        // ── Endpoints ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the entity list for the specified game map, or an empty array if no entities are defined.
        /// </summary>
        /// <param name="game">The game identifier (e.g. ODOOM, OQUAKE).</param>
        /// <param name="map">The map identifier (e.g. E1M3, e2m3).</param>
        /// <returns>List of MapEntity objects placed on that map.</returns>
        /// <response code="200">Entity list returned (may be empty)</response>
        [HttpGet("{game}/{map}/entities")]
        [ProducesResponseType(typeof(IEnumerable<MapEntity>), StatusCodes.Status200OK)]
        public IActionResult GetEntities(string game, string map)
        {
            try
            {
                var key = MapKey(game, map);
                _logger.LogInformation("[MapEntities] GET entities for {Key}.", key);

                lock (_lock)
                {
                    if (_entities.TryGetValue(key, out var list))
                        return Ok(list);
                }

                return Ok(Array.Empty<MapEntity>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MapEntities] Error retrieving entities for {Game}/{Map}.", game, map);
                return StatusCode(500, new { error = "Internal server error retrieving map entities." });
            }
        }

        /// <summary>
        /// Replaces the entity list for the specified map and persists it to
        /// Config/map_entities/{game}_{map}.json.
        /// </summary>
        /// <param name="game">The game identifier.</param>
        /// <param name="map">The map identifier.</param>
        /// <param name="entities">The complete replacement entity list for this map.</param>
        /// <returns>The updated entity list.</returns>
        /// <response code="200">Entity list updated and persisted</response>
        /// <response code="400">Request body is null</response>
        [HttpPut("{game}/{map}/entities")]
        [ProducesResponseType(typeof(IEnumerable<MapEntity>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult PutEntities(string game, string map, [FromBody] List<MapEntity> entities)
        {
            try
            {
                if (entities == null)
                    return BadRequest(new { error = "Request body (entity list) is required." });

                var key = MapKey(game, map);

                lock (_lock)
                {
                    _entities[key] = entities;
                }

                // Persist to disk
                var dir = MapEntitiesDir;
                Directory.CreateDirectory(dir);
                var safeName = string.Concat(key.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                var filePath = Path.Combine(dir, $"{safeName}.json");
                var json = JsonSerializer.Serialize(entities, _jsonOpts);
                File.WriteAllText(filePath, json);

                _logger.LogInformation(
                    "[MapEntities] Updated {Count} entities for {Key}, persisted to {FilePath}.",
                    entities.Count, key, filePath);

                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MapEntities] Error updating entities for {Game}/{Map}.", game, map);
                return StatusCode(500, new { error = "Internal server error updating map entities." });
            }
        }
    }
}
