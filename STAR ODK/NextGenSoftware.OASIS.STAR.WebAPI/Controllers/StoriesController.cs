using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    // ─────────────────────────────────────────────
    // Story arc data model (snake_case to match JSON)
    // ─────────────────────────────────────────────

    /// <summary>A cross-game spawn triggered as part of a story chapter.</summary>
    public record CrossSpawn(string game, string entity, int count);

    /// <summary>One chapter in a StoryArc: a game map, a trigger condition, and an optional reward/narration.</summary>
    public record StoryChapter(
        string game,
        string map,
        string trigger,
        string reward,
        string? narration,
        CrossSpawn? cross_spawn
    );

    /// <summary>A cross-game story arc composed of ordered chapters across multiple OASIS games.</summary>
    public record StoryArc(string story_id, string title, List<StoryChapter> chapters);

    // ─────────────────────────────────────────────
    // Controller
    // ─────────────────────────────────────────────

    /// <summary>
    /// Manages cross-game story arcs. Story arcs are loaded from *.json files in the
    /// OASIS Omniverse Config/stories/ directory on startup and kept in memory.
    /// New arcs POSTed to this endpoint are persisted back to that directory.
    /// </summary>
    [ApiController]
    [Route("api/stories")]
    public class StoriesController : ControllerBase
    {
        private static readonly List<StoryArc> _stories = new();
        private static bool _loaded = false;
        private static readonly object _lock = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly ILogger<StoriesController> _logger;
        private readonly IWebHostEnvironment _env;

        public StoriesController(ILogger<StoriesController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
            EnsureLoaded();
        }

        // ── Path helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the OASIS Omniverse Config/stories/ directory.
        /// Navigates two levels up from the WebAPI content root to reach the OASIS2 monorepo root,
        /// then descends into "OASIS Omniverse/Config/stories".
        /// Falls back gracefully if the directory does not exist.
        /// </summary>
        private string StoriesDir
        {
            get
            {
                var oasis2Root = Path.GetDirectoryName(Path.GetDirectoryName(_env.ContentRootPath));
                return Path.Combine(oasis2Root ?? AppContext.BaseDirectory, "OASIS Omniverse", "Config", "stories");
            }
        }

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
            var dir = StoriesDir;
            if (!Directory.Exists(dir))
            {
                _logger.LogInformation("[Stories] Config/stories/ directory not found at {Dir}; starting with empty list.", dir);
                Directory.CreateDirectory(dir);
                return;
            }

            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var arc = JsonSerializer.Deserialize<StoryArc>(json, _jsonOpts);
                    if (arc != null)
                    {
                        _stories.Add(arc);
                        _logger.LogInformation("[Stories] Loaded story arc '{StoryId}' from {File}.", arc.story_id, file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Stories] Failed to parse story arc from {File}; skipping.", file);
                }
            }

            _logger.LogInformation("[Stories] Loaded {Count} story arc(s).", _stories.Count);
        }

        // ── Endpoints ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all loaded story arcs.
        /// </summary>
        /// <returns>List of all cross-game story arcs.</returns>
        /// <response code="200">Story arcs retrieved successfully</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<StoryArc>), StatusCodes.Status200OK)]
        public IActionResult GetAll()
        {
            try
            {
                _logger.LogInformation("[Stories] GET all ({Count} arcs).", _stories.Count);
                return Ok(_stories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Stories] Error retrieving all story arcs.");
                return StatusCode(500, new { error = "Internal server error retrieving story arcs." });
            }
        }

        /// <summary>
        /// Returns a single story arc by its story_id.
        /// </summary>
        /// <param name="id">The story_id of the arc to retrieve (e.g. oasis_arc_001).</param>
        /// <returns>The matching StoryArc, or 404 if not found.</returns>
        /// <response code="200">Story arc found</response>
        /// <response code="404">No story arc with that id</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(StoryArc), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetById(string id)
        {
            try
            {
                var arc = _stories.FirstOrDefault(s =>
                    string.Equals(s.story_id, id, StringComparison.OrdinalIgnoreCase));

                if (arc == null)
                {
                    _logger.LogInformation("[Stories] Story arc '{Id}' not found.", id);
                    return NotFound(new { error = $"Story arc '{id}' not found." });
                }

                return Ok(arc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Stories] Error retrieving story arc '{Id}'.", id);
                return StatusCode(500, new { error = "Internal server error retrieving story arc." });
            }
        }

        /// <summary>
        /// Adds a new story arc to the in-memory list and persists it as a JSON file
        /// in Config/stories/{story_id}.json.
        /// </summary>
        /// <param name="arc">The story arc to add. story_id must be unique.</param>
        /// <returns>The created story arc.</returns>
        /// <response code="200">Story arc added successfully</response>
        /// <response code="400">Body is null, story_id is missing, or a duplicate story_id already exists</response>
        [HttpPost]
        [ProducesResponseType(typeof(StoryArc), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Add([FromBody] StoryArc arc)
        {
            try
            {
                if (arc == null)
                    return BadRequest(new { error = "Request body is required." });

                if (string.IsNullOrWhiteSpace(arc.story_id))
                    return BadRequest(new { error = "story_id is required." });

                lock (_lock)
                {
                    if (_stories.Any(s => string.Equals(s.story_id, arc.story_id, StringComparison.OrdinalIgnoreCase)))
                        return BadRequest(new { error = $"A story arc with id '{arc.story_id}' already exists." });

                    _stories.Add(arc);
                }

                // Persist to disk
                var dir = StoriesDir;
                Directory.CreateDirectory(dir);
                var safeName = string.Concat(arc.story_id.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                var filePath = Path.Combine(dir, $"{safeName}.json");
                var json = JsonSerializer.Serialize(arc, _jsonOpts);
                File.WriteAllText(filePath, json);

                _logger.LogInformation("[Stories] Added and persisted story arc '{StoryId}' to {FilePath}.", arc.story_id, filePath);
                return Ok(arc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Stories] Error adding story arc '{StoryId}'.", arc?.story_id ?? "(null)");
                return StatusCode(500, new { error = "Internal server error adding story arc." });
            }
        }
    }
}
