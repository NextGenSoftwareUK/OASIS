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
    /// <summary>An entity placed on a game map with a position.</summary>
    public record MapEntity(string entityId, float x, float y, float z);

    /// <summary>
    /// Manages per-map OASIS entity placements as Holons (WEB4) / SmartBricks (WEB5).
    ///
    /// Each map's entity list is a STARNETHolon keyed by game + map, stored via HolonManager.
    /// On first request for a map, any Config/map_entities/{game}_{map}.json seed file is imported.
    /// </summary>
    [ApiController]
    [Route("api/maps")]
    public class MapEntitiesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());
        private static readonly SemaphoreSlim _seedLock = new(1, 1);
        private static bool _seeded = false;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly ILogger<MapEntitiesController> _logger;
        private readonly IWebHostEnvironment _env;

        protected override STARAPI GetStarAPI() => _starAPI;

        public MapEntitiesController(ILogger<MapEntitiesController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        // ── Path helpers ─────────────────────────────────────────────────────────

        private string MapEntitiesDir
        {
            get
            {
                var oasis2Root = Path.GetDirectoryName(Path.GetDirectoryName(_env.ContentRootPath));
                return Path.Combine(oasis2Root ?? AppContext.BaseDirectory, "OASIS Omniverse", "Config", "map_entities");
            }
        }

        private static string MapKey(string game, string map) => $"{game.ToUpperInvariant()}_{map.ToUpperInvariant()}";

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

                var existing = await _starAPI.Holons.LoadAllAsync(AvatarId, STARHolonType.MapEntityList, loadAllTypes: false);
                if (existing.IsError || existing.Result == null || !existing.Result.Any())
                {
                    _logger.LogInformation("[MapEntities] No MapEntityList Holons found; seeding from Config/map_entities/.");
                    await SeedFromJsonAsync();
                }

                _seeded = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MapEntities] Seed check failed; continuing without seeding.");
                _seeded = true;
            }
            finally
            {
                _seedLock.Release();
            }
        }

        private async Task SeedFromJsonAsync()
        {
            var dir = MapEntitiesDir;
            if (!Directory.Exists(dir))
            {
                _logger.LogInformation("[MapEntities] Config/map_entities/ not found at {Dir}; skipping seed.", dir);
                return;
            }

            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var key = Path.GetFileNameWithoutExtension(file); // e.g. "ODOOM_E1M3"
                    var parts = key.Split('_', 2);
                    if (parts.Length < 2) continue;

                    var game = parts[0];
                    var map = parts[1];
                    var json = File.ReadAllText(file);
                    var entities = JsonSerializer.Deserialize<List<MapEntity>>(json, _jsonOpts);
                    if (entities == null) continue;

                    var holon = EntitiesToHolon(game, map, entities);
                    var result = await _starAPI.Holons.UpdateAsync(AvatarId, holon);
                    if (result.IsError)
                        _logger.LogWarning("[MapEntities] Seed failed for '{Key}': {Msg}", key, result.Message);
                    else
                        _logger.LogInformation("[MapEntities] Seeded '{Key}' ({Count} entities) → Holon {Id}.", key, entities.Count, result.Result?.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[MapEntities] Seed parse error for {File}; skipping.", file);
                }
            }
        }

        // ── Model ↔ Holon mapping ────────────────────────────────────────────────

        private static STARHolon EntitiesToHolon(string game, string map, List<MapEntity> entities, Guid? existingId = null)
        {
            return new STARHolon
            {
                Id = existingId ?? Guid.NewGuid(),
                Name = $"{game.ToUpperInvariant()}_{map.ToUpperInvariant()} Entities",
                Description = $"OASIS entity placements for {game}/{map}",
                HolonType = HolonType.STARNETHolon,
                MetaData = new Dictionary<string, object>
                {
                    // "HolonType" is the MetaData key used by STARHolonManager.LoadHolonsByMetaDataAsync filter
                    ["HolonType"] = nameof(STARHolonType.MapEntityList),
                    ["Game"] = game.ToUpperInvariant(),
                    ["Map"] = map.ToUpperInvariant(),
                    ["EntitiesJson"] = JsonSerializer.Serialize(entities, _jsonOpts)
                }
            };
        }

        private static List<MapEntity> HolonToEntities(STARHolon? holon)
        {
            if (holon?.MetaData == null || !holon.MetaData.TryGetValue("EntitiesJson", out var raw))
                return new List<MapEntity>();
            try { return JsonSerializer.Deserialize<List<MapEntity>>(raw?.ToString() ?? "[]", _jsonOpts) ?? new(); }
            catch { return new(); }
        }

        private static string? GetMeta(STARHolon h, string key)
            => h.MetaData?.TryGetValue(key, out var v) == true ? v?.ToString() : null;

        // ── Endpoints ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the entity list for the specified game map.
        /// Used by ogengine_get_map_entities() at map load time.
        /// </summary>
        [HttpGet("{game}/{map}/entities")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<MapEntity>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEntities(string game, string map)
        {
            try
            {
                await EnsureSeededAsync();

                var allResult = await _starAPI.Holons.LoadAllAsync(AvatarId, STARHolonType.MapEntityList, loadAllTypes: false);
                if (allResult.IsError)
                    return BadRequest(new OASISResult<IEnumerable<MapEntity>> { IsError = true, Message = allResult.Message });

                var match = (allResult.Result ?? Enumerable.Empty<STARHolon>()).FirstOrDefault(h =>
                    string.Equals(GetMeta(h, "Game"), game, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(GetMeta(h, "Map"), map, StringComparison.OrdinalIgnoreCase));

                var entities = match != null ? HolonToEntities(match) : new List<MapEntity>();
                _logger.LogInformation("[MapEntities] GET {Game}/{Map} → {Count} entities.", game, map, entities.Count);

                return Ok(new OASISResult<IEnumerable<MapEntity>>
                {
                    Result = entities,
                    IsError = false,
                    Message = $"{entities.Count} entities for {game}/{map}."
                });
            }
            catch (Exception ex)
            {
                return HandleException<IEnumerable<MapEntity>>(ex, $"GetEntities {game}/{map}");
            }
        }

        /// <summary>
        /// Replaces the entity list for the specified map. The new list is stored as a Holon.
        /// Used by UDB map export / OASIS entity placement tools.
        /// </summary>
        [HttpPut("{game}/{map}/entities")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<MapEntity>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<MapEntity>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PutEntities(string game, string map, [FromBody] List<MapEntity> entities)
        {
            try
            {
                if (entities == null)
                    return BadRequest(new OASISResult<IEnumerable<MapEntity>> { IsError = true, Message = "Request body (entity list) is required." });

                var avatarCheck = ValidateAvatarId<IEnumerable<MapEntity>>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                // Find existing holon for this map so we update rather than create a duplicate
                var allResult = await _starAPI.Holons.LoadAllAsync(AvatarId, STARHolonType.MapEntityList, loadAllTypes: false);
                Guid? existingId = null;
                if (!allResult.IsError && allResult.Result != null)
                {
                    var existing = allResult.Result.FirstOrDefault(h =>
                        string.Equals(GetMeta(h, "Game"), game, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(GetMeta(h, "Map"), map, StringComparison.OrdinalIgnoreCase));
                    existingId = existing?.Id;
                }

                var holon = EntitiesToHolon(game, map, entities, existingId);
                var result = await _starAPI.Holons.UpdateAsync(AvatarId, holon);
                if (result.IsError)
                    return BadRequest(new OASISResult<IEnumerable<MapEntity>> { IsError = true, Message = result.Message });

                _logger.LogInformation("[MapEntities] PUT {Game}/{Map} → {Count} entities, Holon {Id}.", game, map, entities.Count, result.Result?.Id);
                return Ok(new OASISResult<IEnumerable<MapEntity>>
                {
                    Result = entities,
                    IsError = false,
                    Message = $"{entities.Count} entities saved for {game}/{map}."
                });
            }
            catch (Exception ex)
            {
                return HandleException<IEnumerable<MapEntity>>(ex, $"PutEntities {game}/{map}");
            }
        }
    }
}
