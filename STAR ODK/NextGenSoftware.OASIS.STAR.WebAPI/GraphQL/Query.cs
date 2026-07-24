using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.GraphQL.Types;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GraphQL
{
    /// <summary>Root Query type for the STAR ODK GraphQL endpoint.</summary>
    public class Query
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        // ── STAR Status ───────────────────────────────────────────────────────

        public bool GetStarStatus() => _starAPI.IsOASISBooted;

        // ── Celestial Bodies ──────────────────────────────────────────────────

        public async Task<IEnumerable<CelestialBodyDto>> GetCelestialBodiesAsync()
        {
            var result = await _starAPI.CelestialBodies.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<CelestialBodyDto>();

            return result.Result.Select(b => new CelestialBodyDto
            {
                Id = b.Id.ToString(),
                Name = b.Name ?? string.Empty,
                Description = b.Description ?? string.Empty,
            });
        }

        public async Task<CelestialBodyDto?> GetCelestialBodyAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var result = await _starAPI.CelestialBodies.LoadAsync(Guid.Empty, guid, 0);
            if (result.IsError || result.Result == null)
                return null;

            var b = result.Result;
            return new CelestialBodyDto { Id = b.Id.ToString(), Name = b.Name ?? string.Empty, Description = b.Description ?? string.Empty };
        }

        // ── Celestial Spaces ──────────────────────────────────────────────────

        public async Task<IEnumerable<CelestialSpaceDto>> GetCelestialSpacesAsync()
        {
            var result = await _starAPI.CelestialSpaces.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<CelestialSpaceDto>();

            return result.Result.Select(s => new CelestialSpaceDto
            {
                Id = s.Id.ToString(),
                Name = s.Name ?? string.Empty,
                Description = s.Description ?? string.Empty,
            });
        }

        public async Task<CelestialSpaceDto?> GetCelestialSpaceAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var result = await _starAPI.CelestialSpaces.LoadAsync(Guid.Empty, guid, 0);
            if (result.IsError || result.Result == null)
                return null;

            var s = result.Result;
            return new CelestialSpaceDto { Id = s.Id.ToString(), Name = s.Name ?? string.Empty, Description = s.Description ?? string.Empty };
        }

        // ── NFTs ──────────────────────────────────────────────────────────────

        public async Task<IEnumerable<NftDto>> GetNftsAsync()
        {
            var result = await _starAPI.NFTs.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<NftDto>();

            return result.Result.Select(n => new NftDto
            {
                Id = n.Id.ToString(),
                Name = n.Name ?? string.Empty,
                Description = n.Description ?? string.Empty,
            });
        }

        public async Task<NftDto?> GetNftAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var result = await _starAPI.NFTs.LoadAsync(Guid.Empty, guid, 0);
            if (result.IsError || result.Result == null)
                return null;

            var n = result.Result;
            return new NftDto { Id = n.Id.ToString(), Name = n.Name ?? string.Empty, Description = n.Description ?? string.Empty };
        }

        // ── Holons ────────────────────────────────────────────────────────────

        public async Task<IEnumerable<HolonDto>> GetHolonsAsync()
        {
            var result = await _starAPI.Holons.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<HolonDto>();

            return result.Result.Select(h => new HolonDto
            {
                Id = h.Id.ToString(),
                Name = h.Name ?? string.Empty,
                Description = h.Description ?? string.Empty,
                HolonType = h.HolonType.ToString(),
                Status = h.IsActive ? "Active" : "Inactive",
            });
        }

        public async Task<HolonDto?> GetHolonAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var result = await _starAPI.Holons.LoadAsync(Guid.Empty, guid, 0);
            if (result.IsError || result.Result == null)
                return null;

            var h = result.Result;
            return new HolonDto
            {
                Id = h.Id.ToString(),
                Name = h.Name ?? string.Empty,
                Description = h.Description ?? string.Empty,
                HolonType = h.HolonType.ToString(),
                Status = h.IsActive ? "Active" : "Inactive",
            };
        }

        // ── Quests ────────────────────────────────────────────────────────────

        public async Task<IEnumerable<QuestDto>> GetQuestsAsync()
        {
            var result = await _starAPI.Quests.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<QuestDto>();

            return result.Result.Select(q => new QuestDto
            {
                Id = q.Id.ToString(),
                Name = q.Name ?? string.Empty,
                Description = q.Description ?? string.Empty,
                QuestType = q.QuestType.ToString(),
            });
        }

        public async Task<QuestDto?> GetQuestAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var result = await _starAPI.Quests.LoadAsync(Guid.Empty, guid, 0);
            if (result.IsError || result.Result == null)
                return null;

            var q = result.Result;
            return new QuestDto
            {
                Id = q.Id.ToString(),
                Name = q.Name ?? string.Empty,
                Description = q.Description ?? string.Empty,
                QuestType = q.QuestType.ToString(),
            };
        }

        // ── Missions ──────────────────────────────────────────────────────────

        public async Task<IEnumerable<MissionDto>> GetMissionsAsync()
        {
            var result = await _starAPI.Missions.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<MissionDto>();

            return result.Result.Select(m => new MissionDto
            {
                Id = m.Id.ToString(),
                Name = m.Name ?? string.Empty,
                Description = m.Description ?? string.Empty,
            });
        }

        public async Task<MissionDto?> GetMissionAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var result = await _starAPI.Missions.LoadAsync(Guid.Empty, guid, 0);
            if (result.IsError || result.Result == null)
                return null;

            var m = result.Result;
            return new MissionDto { Id = m.Id.ToString(), Name = m.Name ?? string.Empty, Description = m.Description ?? string.Empty };
        }

        // ── Chapters ──────────────────────────────────────────────────────────

        public async Task<IEnumerable<ChapterDto>> GetChaptersAsync()
        {
            var result = await _starAPI.Chapters.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<ChapterDto>();

            return result.Result.Select(c => new ChapterDto
            {
                Id = c.Id.ToString(),
                Name = c.Name ?? string.Empty,
                Description = c.Description ?? string.Empty,
            });
        }

        // ── GeoHotSpots ───────────────────────────────────────────────────────

        public async Task<IEnumerable<GeoHotSpotDto>> GetGeoHotSpotsAsync()
        {
            var result = await _starAPI.GeoHotSpots.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<GeoHotSpotDto>();

            return result.Result.Select(g => new GeoHotSpotDto
            {
                Id = g.Id.ToString(),
                Name = g.Name ?? string.Empty,
                Description = g.Description ?? string.Empty,
                Lat = g.Lat,
                Long = g.Long,
            });
        }

        public async Task<GeoHotSpotDto?> GetGeoHotSpotAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var result = await _starAPI.GeoHotSpots.LoadAsync(Guid.Empty, guid, 0);
            if (result.IsError || result.Result == null)
                return null;

            var g = result.Result;
            return new GeoHotSpotDto { Id = g.Id.ToString(), Name = g.Name ?? string.Empty, Description = g.Description ?? string.Empty, Lat = g.Lat, Long = g.Long };
        }

        // ── GeoNFTs ───────────────────────────────────────────────────────────

        public async Task<IEnumerable<GeoNFTDto>> GetGeoNFTsAsync()
        {
            var result = await _starAPI.GeoNFTs.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<GeoNFTDto>();

            return result.Result.Select(g => new GeoNFTDto
            {
                Id = g.Id.ToString(),
                Name = g.Name ?? string.Empty,
                Description = g.Description ?? string.Empty,
                NftType = g.NFTType.ToString(),
            });
        }

        // ── InventoryItems ────────────────────────────────────────────────────

        public async Task<IEnumerable<InventoryItemDto>> GetInventoryItemsAsync()
        {
            var result = await _starAPI.InventoryItems.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<InventoryItemDto>();

            return result.Result.Select(i => new InventoryItemDto
            {
                Id = i.Id.ToString(),
                Name = i.Name ?? string.Empty,
                Description = i.Description ?? string.Empty,
            });
        }

        // ── OAPPs ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<OAPPDto>> GetOAPPsAsync()
        {
            var result = await _starAPI.OAPPs.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<OAPPDto>();

            return result.Result.Select(o => new OAPPDto
            {
                Id = o.Id.ToString(),
                Name = o.Name ?? string.Empty,
                Description = o.Description ?? string.Empty,
            });
        }

        public async Task<OAPPDto?> GetOAPPAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var result = await _starAPI.OAPPs.LoadAsync(Guid.Empty, guid, 0);
            if (result.IsError || result.Result == null)
                return null;

            var o = result.Result;
            return new OAPPDto { Id = o.Id.ToString(), Name = o.Name ?? string.Empty, Description = o.Description ?? string.Empty };
        }

        // ── Games ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<GameDto>> GetGamesAsync()
        {
            var result = await _starAPI.Game.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<GameDto>();

            return result.Result.Select(g => new GameDto
            {
                Id = g.Id.ToString(),
                Name = g.Name ?? string.Empty,
                Description = g.Description ?? string.Empty,
            });
        }

        public async Task<GameDto?> GetGameAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var result = await _starAPI.Game.LoadAsync(Guid.Empty, guid, 0);
            if (result.IsError || result.Result == null)
                return null;

            var g = result.Result;
            return new GameDto { Id = g.Id.ToString(), Name = g.Name ?? string.Empty, Description = g.Description ?? string.Empty };
        }

        // ── Plugins ───────────────────────────────────────────────────────────

        public async Task<IEnumerable<PluginDto>> GetPluginsAsync()
        {
            var result = await _starAPI.Plugins.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<PluginDto>();

            return result.Result.Select(p => new PluginDto
            {
                Id = p.Id.ToString(),
                Name = p.Name ?? string.Empty,
                Description = p.Description ?? string.Empty,
            });
        }

        // ── Libraries ─────────────────────────────────────────────────────────

        public async Task<IEnumerable<LibraryDto>> GetLibrariesAsync()
        {
            var result = await _starAPI.Libraries.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<LibraryDto>();

            return result.Result.Select(l => new LibraryDto
            {
                Id = l.Id.ToString(),
                Name = l.Name ?? string.Empty,
                Description = l.Description ?? string.Empty,
            });
        }

        // ── Runtimes ──────────────────────────────────────────────────────────

        public async Task<IEnumerable<RuntimeDto>> GetRuntimesAsync()
        {
            var result = await _starAPI.Runtimes.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<RuntimeDto>();

            return result.Result.Select(r => new RuntimeDto
            {
                Id = r.Id.ToString(),
                Name = r.Name ?? string.Empty,
                Description = r.Description ?? string.Empty,
            });
        }

        // ── OAPP Templates ────────────────────────────────────────────────────

        public async Task<IEnumerable<OAPPTemplateDto>> GetOAPPTemplatesAsync()
        {
            var result = await _starAPI.OAPPTemplates.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<OAPPTemplateDto>();

            return result.Result.Select(t => new OAPPTemplateDto
            {
                Id = t.Id.ToString(),
                Name = t.Name ?? string.Empty,
                Description = t.Description ?? string.Empty,
            });
        }

        // ── Zomes ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<ZomeDto>> GetZomesAsync()
        {
            var result = await _starAPI.Zomes.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<ZomeDto>();

            return result.Result.Select(z => new ZomeDto
            {
                Id = z.Id.ToString(),
                Name = z.Name ?? string.Empty,
                Description = z.Description ?? string.Empty,
            });
        }

        // ── Competition ────────────────────────────────────────────────────────

        public async Task<IEnumerable<LeaderboardEntry>> GetStarLeaderboardAsync(string competitionType, string seasonType, int limit = 100, int offset = 0)
        {
            var ct = Enum.TryParse<CompetitionType>(competitionType, true, out var c) ? c : CompetitionType.Karma;
            var st = Enum.TryParse<SeasonType>(seasonType, true, out var s) ? s : SeasonType.Daily;
            var result = await CompetitionManager.Instance.GetLeaderboardAsync(ct, st, limit, offset);
            return result.IsError || result.Result == null ? Enumerable.Empty<LeaderboardEntry>() : result.Result;
        }

        public async Task<LeaderboardEntry?> GetStarAvatarRankAsync(Guid avatarId, string competitionType, string seasonType)
        {
            var ct = Enum.TryParse<CompetitionType>(competitionType, true, out var c) ? c : CompetitionType.Karma;
            var st = Enum.TryParse<SeasonType>(seasonType, true, out var s) ? s : SeasonType.Daily;
            var result = await CompetitionManager.Instance.GetAvatarRankAsync(avatarId, ct, st);
            return result.IsError ? null : result.Result;
        }

        public async Task<IEnumerable<League>> GetStarLeaguesAsync(string competitionType, string seasonType)
        {
            var ct = Enum.TryParse<CompetitionType>(competitionType, true, out var c) ? c : CompetitionType.Karma;
            var st = Enum.TryParse<SeasonType>(seasonType, true, out var s) ? s : SeasonType.Daily;
            var result = await CompetitionManager.Instance.GetAvailableLeaguesAsync(ct, st);
            return result.IsError || result.Result == null ? Enumerable.Empty<League>() : result.Result;
        }

        public async Task<League?> GetStarAvatarLeagueAsync(Guid avatarId, string competitionType, string seasonType)
        {
            var ct = Enum.TryParse<CompetitionType>(competitionType, true, out var c) ? c : CompetitionType.Karma;
            var st = Enum.TryParse<SeasonType>(seasonType, true, out var s) ? s : SeasonType.Daily;
            var result = await CompetitionManager.Instance.GetAvatarLeagueAsync(avatarId, ct, st);
            return result.IsError ? null : result.Result;
        }

        public async Task<IEnumerable<Tournament>> GetStarTournamentsAsync()
        {
            var result = await CompetitionManager.Instance.GetActiveTournamentsAsync();
            return result.IsError || result.Result == null ? Enumerable.Empty<Tournament>() : result.Result;
        }

        // ── Cosmic (Omniverse hierarchy) ───────────────────────────────────────

        private COSMICManager CreateCosmicManager()
        {
            var result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new COSMICManager(result.Result, Guid.Empty, OASISBootLoader.OASISBootLoader.OASISDNA);
        }

        public object? GetOmniverse()
        {
            try { return CreateCosmicManager().Omiverse; }
            catch { return null; }
        }

        public async Task<IEnumerable<object>> GetChildrenForParentAsync(Guid parentId, string parentHolonType, string childHolonType)
        {
            if (!Enum.TryParse<HolonType>(parentHolonType, true, out var pht)) pht = HolonType.All;
            if (!Enum.TryParse<HolonType>(childHolonType, true, out var cht)) cht = HolonType.All;
            var mgr = CreateCosmicManager();
            var result = await mgr.GetChildrenForParentAsync<NextGenSoftware.OASIS.API.Core.Holons.Holon>(parentId, pht, cht);
            return result.IsError || result.Result == null ? Enumerable.Empty<object>() : result.Result.Cast<object>();
        }

        public async Task<IEnumerable<object>> SearchChildrenForParentAsync(string searchTerm, Guid parentId, string parentHolonType, string childHolonType)
        {
            if (!Enum.TryParse<HolonType>(parentHolonType, true, out var pht)) pht = HolonType.All;
            if (!Enum.TryParse<HolonType>(childHolonType, true, out var cht)) cht = HolonType.All;
            var mgr = CreateCosmicManager();
            var result = await mgr.SearchChildrenForParentAsync(searchTerm, parentId, pht, cht);
            return result.IsError || result.Result == null ? Enumerable.Empty<object>() : result.Result.Cast<object>();
        }

        public async Task<IEnumerable<object>> SearchHolonsForParentAsync(string searchTerm, Guid parentId, string parentHolonType, string childHolonType)
        {
            if (!Enum.TryParse<HolonType>(parentHolonType, true, out var pht)) pht = HolonType.All;
            if (!Enum.TryParse<HolonType>(childHolonType, true, out var cht)) cht = HolonType.All;
            var mgr = CreateCosmicManager();
            var result = await mgr.SearchHolonsForParentAsync<NextGenSoftware.OASIS.API.Core.Holons.Holon>(searchTerm, Guid.Empty, parentId, holonType: cht);
            return result.IsError || result.Result == null ? Enumerable.Empty<object>() : result.Result.Cast<object>();
        }

        public async Task<IEnumerable<object>> GetPlanetsForSolarSystemAsync(Guid solarSystemId)
        {
            var mgr = CreateCosmicManager();
            var result = await mgr.GetPlanetsForSolarSystemAsync(solarSystemId);
            return result.IsError || result.Result == null ? Enumerable.Empty<object>() : result.Result.Cast<object>();
        }

        public async Task<IEnumerable<object>> GetSolarSystemsForGalaxyAsync(Guid galaxyId)
        {
            var mgr = CreateCosmicManager();
            var result = await mgr.GetSolarSystemsForGalaxyAsync(galaxyId);
            return result.IsError || result.Result == null ? Enumerable.Empty<object>() : result.Result.Cast<object>();
        }

        public async Task<IEnumerable<object>> GetStarsForGalaxyAsync(Guid galaxyId)
        {
            var mgr = CreateCosmicManager();
            var result = await mgr.GetStarsForGalaxyAsync(galaxyId);
            return result.IsError || result.Result == null ? Enumerable.Empty<object>() : result.Result.Cast<object>();
        }

        public async Task<IEnumerable<object>> GetPlanetsForGalaxyAsync(Guid galaxyId)
        {
            var mgr = CreateCosmicManager();
            var result = await mgr.GetPlanetsForGalaxyAsync(galaxyId);
            return result.IsError || result.Result == null ? Enumerable.Empty<object>() : result.Result.Cast<object>();
        }

        public async Task<IEnumerable<object>> GetMoonsForGalaxyAsync(Guid galaxyId)
        {
            var mgr = CreateCosmicManager();
            var result = await mgr.GetMoonsForGalaxyAsync(galaxyId);
            return result.IsError || result.Result == null ? Enumerable.Empty<object>() : result.Result.Cast<object>();
        }

        // ── CelestialBodyMetaData ─────────────────────────────────────────────
        public async Task<string> GetAllCelestialBodiesMetaDataAsync()
        {
            var result = await _starAPI.CelestialBodiesMetaDataDNA.LoadAllAsync(Guid.Empty, null);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }

        public async Task<string> GetCelestialBodyMetaDataAsync(Guid id)
        {
            var result = await _starAPI.CelestialBodiesMetaDataDNA.LoadAsync(Guid.Empty, id, 0);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }

        public async Task<string> GetCelestialBodyMetaDataVersionsAsync(Guid id)
        {
            var result = await _starAPI.CelestialBodiesMetaDataDNA.LoadVersionsAsync(id);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }

        public async Task<string> SearchCelestialBodiesMetaDataAsync(string searchTerm, bool showAllVersions = false, int version = 0)
        {
            var result = await _starAPI.CelestialBodiesMetaDataDNA.SearchAsync<NextGenSoftware.OASIS.API.ONODE.Core.Holons.CelestialBodyMetaDataDNA>(Guid.Empty, searchTerm, default, null, NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All, true, showAllVersions, version);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }

        // ── HolonMetaData ─────────────────────────────────────────────────────
        public async Task<string> GetAllHolonsMetaDataAsync()
        {
            var result = await _starAPI.HolonsMetaDataDNA.LoadAllAsync(Guid.Empty, null);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }

        public async Task<string> GetHolonMetaDataAsync(Guid id)
        {
            var result = await _starAPI.HolonsMetaDataDNA.LoadAsync(Guid.Empty, id, 0);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }

        public async Task<string> GetHolonMetaDataVersionsAsync(Guid id)
        {
            var result = await _starAPI.HolonsMetaDataDNA.LoadVersionsAsync(id);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }

        public async Task<string> SearchHolonsMetaDataAsync(string searchTerm, bool showAllVersions = false, int version = 0)
        {
            var result = await _starAPI.HolonsMetaDataDNA.SearchAsync<NextGenSoftware.OASIS.API.ONODE.Core.Holons.HolonMetaDataDNA>(Guid.Empty, searchTerm, default, null, NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All, true, showAllVersions, version);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }

        // ── ZomeMetaData ──────────────────────────────────────────────────────
        public async Task<string> GetAllZomesMetaDataAsync()
        {
            var result = await _starAPI.ZomesMetaDataDNA.LoadAllAsync(Guid.Empty, null);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }

        public async Task<string> GetZomeMetaDataAsync(Guid id)
        {
            var result = await _starAPI.ZomesMetaDataDNA.LoadAsync(Guid.Empty, id, 0);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }

        public async Task<string> GetZomeMetaDataVersionsAsync(Guid id)
        {
            var result = await _starAPI.ZomesMetaDataDNA.LoadVersionsAsync(id);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }

        public async Task<string> SearchZomesMetaDataAsync(string searchTerm, bool showAllVersions = false, int version = 0)
        {
            var result = await _starAPI.ZomesMetaDataDNA.SearchAsync<NextGenSoftware.OASIS.API.ONODE.Core.Holons.ZomeMetaDataDNA>(Guid.Empty, searchTerm, default, null, NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All, true, showAllVersions, version);
            return result.IsError ? null : System.Text.Json.JsonSerializer.Serialize(result.Result);
        }
    }
}
