using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    // ─────────────────────────────────────────────
    // Story arc data model (snake_case to match JSON seed files)
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
    /// Manages cross-game story arcs as OASIS Holons (WEB4) / SmartBricks (WEB5).
    ///
    /// Story arcs are stored via HolonManager so they can be:
    ///   - Discovered by any OASIS instance via STARNET
    ///   - Published/downloaded/composed like LEGO bricks
    ///   - Versioned and stored decentrally (IPFS etc.)
    ///
    /// On first request, any *.json files in Config/stories/ are imported as Holons
    /// (seed/import format — JSON is NOT the canonical store).
    /// </summary>
    [ApiController]
    [Route("api/stories")]
    public class StoriesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());
        private static readonly SemaphoreSlim _seedLock = new(1, 1);
        private static bool _seeded = false;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly ILogger<StoriesController> _logger;
        private readonly IWebHostEnvironment _env;

        protected override STARAPI GetStarAPI() => _starAPI;

        public StoriesController(ILogger<StoriesController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        // ── Path helpers ─────────────────────────────────────────────────────────

        private string StoriesDir
        {
            get
            {
                var oasis2Root = Path.GetDirectoryName(Path.GetDirectoryName(_env.ContentRootPath));
                return Path.Combine(oasis2Root ?? AppContext.BaseDirectory, "OASIS Omniverse", "Config", "stories");
            }
        }

        // ── Seed on first request ─────────────────────────────────────────────────

        private async Task EnsureSeededAsync()
        {
            if (_seeded) return;
            await _seedLock.WaitAsync();
            try
            {
                if (_seeded) return;
                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                var existing = await _starAPI.Holons.LoadAllAsync(AvatarId, STARHolonType.StoryArc, loadAllTypes: false);
                if (existing.IsError || existing.Result == null || !existing.Result.Any())
                {
                    _logger.LogInformation("[Stories] No StoryArc Holons found; seeding from Config/stories/.");
                    await SeedFromJsonAsync();
                }

                _seeded = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Stories] Seed check failed; continuing without seeding.");
                _seeded = true;
            }
            finally
            {
                _seedLock.Release();
            }
        }

        private async Task SeedFromJsonAsync()
        {
            var dir = StoriesDir;
            if (!Directory.Exists(dir))
            {
                _logger.LogInformation("[Stories] Config/stories/ not found at {Dir}; skipping seed.", dir);
                return;
            }

            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var arc = JsonSerializer.Deserialize<StoryArc>(json, _jsonOpts);
                    if (arc == null) continue;

                    var holon = ArcToHolon(arc);
                    var result = await _starAPI.Holons.UpdateAsync(AvatarId, holon);
                    if (result.IsError)
                        _logger.LogWarning("[Stories] Seed failed for '{StoryId}': {Msg}", arc.story_id, result.Message);
                    else
                        _logger.LogInformation("[Stories] Seeded '{StoryId}' → Holon {Id}.", arc.story_id, result.Result?.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Stories] Seed parse error for {File}; skipping.", file);
                }
            }
        }

        // ── Model ↔ Holon mapping ────────────────────────────────────────────────

        private static STARHolon ArcToHolon(StoryArc arc, Guid? existingId = null)
        {
            return new STARHolon
            {
                Id = existingId ?? Guid.NewGuid(),
                Name = arc.title,
                Description = $"Cross-game story arc: {arc.story_id}",
                HolonType = HolonType.STARNETHolon,
                MetaData = new Dictionary<string, object>
                {
                    // "HolonType" is the MetaData key used by STARHolonManager.LoadHolonsByMetaDataAsync
                    // to filter holons by STARHolonType enum value.
                    ["HolonType"] = nameof(STARHolonType.StoryArc),
                    ["StoryId"] = arc.story_id,
                    ["ChaptersJson"] = JsonSerializer.Serialize(arc.chapters, _jsonOpts)
                }
            };
        }

        private static StoryArc? HolonToArc(STARHolon? holon)
        {
            if (holon?.MetaData == null) return null;
            if (!holon.MetaData.TryGetValue("StoryId", out var storyIdObj)) return null;
            if (!holon.MetaData.TryGetValue("ChaptersJson", out var chaptersJsonObj)) return null;

            var storyId = storyIdObj?.ToString() ?? "";
            var chaptersJson = chaptersJsonObj?.ToString() ?? "[]";

            List<StoryChapter> chapters;
            try { chapters = JsonSerializer.Deserialize<List<StoryChapter>>(chaptersJson, _jsonOpts) ?? new(); }
            catch { chapters = new(); }

            return new StoryArc(storyId, holon.Name ?? "", chapters);
        }

        // ── Endpoints ───────────────────────────────────────────────────────────

        /// <summary>Returns all story arc Holons.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<StoryArc>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<StoryArc>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                await EnsureSeededAsync();
                var result = await _starAPI.Holons.LoadAllAsync(AvatarId, STARHolonType.StoryArc, loadAllTypes: false);
                if (result.IsError)
                    return BadRequest(new OASISResult<IEnumerable<StoryArc>> { IsError = true, Message = result.Message });

                var arcs = (result.Result ?? Enumerable.Empty<STARHolon>())
                    .Select(HolonToArc).Where(a => a != null).ToList();

                return Ok(new OASISResult<IEnumerable<StoryArc>>
                {
                    Result = arcs!,
                    IsError = false,
                    Message = $"{arcs.Count} story arc(s) retrieved."
                });
            }
            catch (Exception ex)
            {
                return HandleException<IEnumerable<StoryArc>>(ex, "GetAll story arcs");
            }
        }

        /// <summary>Returns a story arc by Holon GUID or story_id slug.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<StoryArc>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                await EnsureSeededAsync();

                if (Guid.TryParse(id, out var holonId))
                {
                    var byGuid = await _starAPI.Holons.LoadAsync(AvatarId, holonId, 0);
                    if (!byGuid.IsError && byGuid.Result != null)
                    {
                        var arc = HolonToArc(byGuid.Result);
                        if (arc != null)
                            return Ok(new OASISResult<StoryArc> { Result = arc, IsError = false });
                    }
                }

                var allResult = await _starAPI.Holons.LoadAllAsync(AvatarId, STARHolonType.StoryArc, loadAllTypes: false);
                if (!allResult.IsError && allResult.Result != null)
                {
                    var match = allResult.Result
                        .Select(h => new { Holon = h, Arc = HolonToArc(h) })
                        .FirstOrDefault(x => x.Arc != null &&
                            string.Equals(x.Arc.story_id, id, StringComparison.OrdinalIgnoreCase));

                    if (match?.Arc != null)
                        return Ok(new OASISResult<StoryArc> { Result = match.Arc, IsError = false });
                }

                return NotFound(new OASISResult<StoryArc> { IsError = true, Message = $"Story arc '{id}' not found." });
            }
            catch (Exception ex)
            {
                return HandleException<StoryArc>(ex, "GetById story arc");
            }
        }

        /// <summary>
        /// Creates a new story arc Holon. The arc is stored via HolonManager and
        /// can be published to STARNET as a SmartBrick via POST {id}/publish.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<StoryArc>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<StoryArc>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] StoryArc arc)
        {
            try
            {
                if (arc == null)
                    return BadRequest(new OASISResult<StoryArc> { IsError = true, Message = "Request body is required." });
                if (string.IsNullOrWhiteSpace(arc.story_id))
                    return BadRequest(new OASISResult<StoryArc> { IsError = true, Message = "story_id is required." });

                var avatarCheck = ValidateAvatarId<StoryArc>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                var holon = ArcToHolon(arc);
                var result = await _starAPI.Holons.UpdateAsync(AvatarId, holon);
                if (result.IsError)
                    return BadRequest(new OASISResult<StoryArc> { IsError = true, Message = result.Message });

                _logger.LogInformation("[Stories] Created StoryArc Holon '{StoryId}' Id={Id}.", arc.story_id, result.Result?.Id);
                return Ok(new OASISResult<StoryArc> { Result = arc, IsError = false, Message = "Story arc created as Holon." });
            }
            catch (Exception ex)
            {
                return HandleException<StoryArc>(ex, "Create story arc");
            }
        }

        /// <summary>Deletes a story arc Holon by its GUID.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var avatarCheck = ValidateAvatarId<bool>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                var result = await _starAPI.Holons.DeleteAsync(AvatarId, id, 0);
                if (result.IsError)
                    return BadRequest(new OASISResult<bool> { IsError = true, Message = result.Message });

                return Ok(new OASISResult<bool> { Result = true, IsError = false, Message = "Story arc deleted." });
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "Delete story arc");
            }
        }
    }
}
