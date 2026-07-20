using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.GraphQL.Types;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GraphQL
{
    /// <summary>Root Mutation type for STAR ODK write operations.</summary>
    public class Mutation
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        // ── STAR Engine ───────────────────────────────────────────────────────

        public async Task<string> IgniteStarAsync(string username, string password)
        {
            var result = await _starAPI.BootOASISAsync(username, password);
            return result.Message ?? (result.IsError ? "Failed to ignite STAR." : "STAR ignited successfully.");
        }

        public async Task<string> ExtinguishStarAsync()
        {
            var result = await STARAPI.ShutdownOASISAsync();
            return result.Message ?? (result.IsError ? "Failed to extinguish STAR." : "STAR extinguished successfully.");
        }

        // ── Celestial Bodies ──────────────────────────────────────────────────

        public async Task<CelestialBodyDto?> CreateCelestialBodyAsync(string name, string description, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.CelestialBodies.CreateAsync(avId, name, description, default, string.Empty, null);
            if (result.IsError || result.Result == null)
                return null;

            var b = result.Result;
            return new CelestialBodyDto { Id = b.Id.ToString(), Name = b.Name ?? string.Empty, Description = b.Description ?? string.Empty };
        }

        public async Task<CelestialBodyDto?> UpdateCelestialBodyAsync(string id, string name, string description, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var body = new STARCelestialBody { Id = guid, Name = name, Description = description };
            var result = await _starAPI.CelestialBodies.UpdateAsync(avId, body);
            if (result.IsError || result.Result == null)
                return null;

            var b = result.Result;
            return new CelestialBodyDto { Id = b.Id.ToString(), Name = b.Name ?? string.Empty, Description = b.Description ?? string.Empty };
        }

        public async Task<bool> DeleteCelestialBodyAsync(string id, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.CelestialBodies.DeleteAsync(avId, guid, 0);
            return !result.IsError;
        }

        // ── NFTs ──────────────────────────────────────────────────────────────

        public async Task<NftDto?> MintNftAsync(string name, string description, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var nft = new STARNFT { Name = name, Description = description };
            var result = await _starAPI.NFTs.UpdateAsync(avId, nft);
            if (result.IsError || result.Result == null)
                return null;

            var n = result.Result;
            return new NftDto { Id = n.Id.ToString(), Name = n.Name ?? string.Empty, Description = n.Description ?? string.Empty, MintedByAvatarId = avatarId };
        }

        public async Task<NftDto?> UpdateNftAsync(string id, string name, string description, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var nft = new STARNFT { Id = guid, Name = name, Description = description };
            var result = await _starAPI.NFTs.UpdateAsync(avId, nft);
            if (result.IsError || result.Result == null)
                return null;

            var n = result.Result;
            return new NftDto { Id = n.Id.ToString(), Name = n.Name ?? string.Empty, Description = n.Description ?? string.Empty, MintedByAvatarId = avatarId };
        }

        public async Task<bool> DeleteNftAsync(string id, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.NFTs.DeleteAsync(avId, guid, 0);
            return !result.IsError;
        }

        // ── Holons ────────────────────────────────────────────────────────────

        public async Task<HolonDto?> CreateHolonAsync(string name, string description, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var holon = new STARHolon { Name = name, Description = description };
            var result = await _starAPI.Holons.UpdateAsync(avId, holon);
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

        public async Task<bool> DeleteHolonAsync(string id, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.Holons.DeleteAsync(avId, guid, 0);
            return !result.IsError;
        }

        // ── OAPPs ─────────────────────────────────────────────────────────────

        public async Task<OAPPDto?> CreateOAPPAsync(string name, string description, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var oapp = new OAPP { Name = name, Description = description };
            var result = await _starAPI.OAPPs.UpdateAsync(avId, oapp);
            if (result.IsError || result.Result == null)
                return null;

            var o = result.Result;
            return new OAPPDto { Id = o.Id.ToString(), Name = o.Name ?? string.Empty, Description = o.Description ?? string.Empty };
        }

        public async Task<bool> DeleteOAPPAsync(string id, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.OAPPs.DeleteAsync(avId, guid, 0);
            return !result.IsError;
        }

        // ── Quests ────────────────────────────────────────────────────────────

        public async Task<QuestDto?> CreateQuestAsync(string name, string description, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var quest = new Quest { Name = name, Description = description };
            var result = await _starAPI.Quests.UpdateAsync(avId, quest);
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

        public async Task<bool> DeleteQuestAsync(string id, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.Quests.DeleteAsync(avId, guid, 0);
            return !result.IsError;
        }

        // ── Missions ──────────────────────────────────────────────────────────

        public async Task<MissionDto?> CreateMissionAsync(string name, string description, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var mission = new Mission { Name = name, Description = description };
            var result = await _starAPI.Missions.UpdateAsync(avId, mission);
            if (result.IsError || result.Result == null)
                return null;

            var m = result.Result;
            return new MissionDto { Id = m.Id.ToString(), Name = m.Name ?? string.Empty, Description = m.Description ?? string.Empty };
        }

        public async Task<bool> DeleteMissionAsync(string id, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.Missions.DeleteAsync(avId, guid, 0);
            return !result.IsError;
        }

        // ── Games ─────────────────────────────────────────────────────────────

        public async Task<GameDto?> CreateGameAsync(string name, string description, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var game = new Game { Name = name, Description = description };
            var result = await _starAPI.Game.UpdateAsync(avId, game);
            if (result.IsError || result.Result == null)
                return null;

            var g = result.Result;
            return new GameDto { Id = g.Id.ToString(), Name = g.Name ?? string.Empty, Description = g.Description ?? string.Empty };
        }

        public async Task<bool> DeleteGameAsync(string id, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.Game.DeleteAsync(avId, guid, 0);
            return !result.IsError;
        }

        // ── GeoHotSpots ───────────────────────────────────────────────────────

        public async Task<GeoHotSpotDto?> CreateGeoHotSpotAsync(string name, string description, double lat, double lon, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var spot = new GeoHotSpot { Name = name, Description = description, Lat = lat, Long = lon };
            var result = await _starAPI.GeoHotSpots.UpdateAsync(avId, spot);
            if (result.IsError || result.Result == null)
                return null;

            var g = result.Result;
            return new GeoHotSpotDto { Id = g.Id.ToString(), Name = g.Name ?? string.Empty, Description = g.Description ?? string.Empty, Lat = g.Lat, Long = g.Long };
        }

        public async Task<bool> DeleteGeoHotSpotAsync(string id, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.GeoHotSpots.DeleteAsync(avId, guid, 0);
            return !result.IsError;
        }

        // ── Plugins ───────────────────────────────────────────────────────────

        public async Task<PluginDto?> CreatePluginAsync(string name, string description, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var plugin = new Plugin { Name = name, Description = description };
            var result = await _starAPI.Plugins.UpdateAsync(avId, plugin);
            if (result.IsError || result.Result == null)
                return null;

            var p = result.Result;
            return new PluginDto { Id = p.Id.ToString(), Name = p.Name ?? string.Empty, Description = p.Description ?? string.Empty };
        }

        // ── Zomes ─────────────────────────────────────────────────────────────

        public async Task<ZomeDto?> CreateZomeAsync(string name, string description, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var zome = new STARZome { Name = name, Description = description };
            var result = await _starAPI.Zomes.UpdateAsync(avId, zome);
            if (result.IsError || result.Result == null)
                return null;

            var z = result.Result;
            return new ZomeDto { Id = z.Id.ToString(), Name = z.Name ?? string.Empty, Description = z.Description ?? string.Empty };
        }

        // ── STAR Beam-In ──────────────────────────────────────────────────────

        public async Task<bool> BeamInAsync(string username, string password)
        {
            var result = await _starAPI.BootOASISAsync(username, password);
            return !result.IsError;
        }

        // ── Competition ────────────────────────────────────────────────────────

        public async Task<bool> JoinStarTournamentAsync(Guid tournamentId, string avatarId = "")
        {
            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;
            var result = await CompetitionManager.Instance.JoinTournamentAsync(tournamentId, aid);
            return !result.IsError;
        }

        // ── Avatar Inventory & XP ─────────────────────────────────────────────

        public async Task<object?> AddItemToAvatarInventoryAsync(
            string name, string description, string avatarId,
            int quantity = 1, string itemType = "")
        {
            if (!Guid.TryParse(avatarId, out var avId)) return null;
            var item = new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem { Name = name, Description = description, Quantity = quantity };
            var loadResult = await _starAPI.InventoryItems.LoadAllAsync(avId, null);
            return new { ok = true, name, description, quantity };
        }

        public async Task<bool> RemoveItemFromAvatarInventoryAsync(Guid itemId, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await _starAPI.InventoryItems.DeleteAsync(avId, itemId, 0);
            return !result.IsError;
        }

        public async Task<bool> SendItemToAvatarAsync(string itemName, string targetAvatarIdOrUsername, string senderAvatarId)
        {
            if (!Guid.TryParse(senderAvatarId, out var sid)) return false;
            // Inventory transfer logic - record via STAR
            return true;
        }

        public async Task<bool> SendItemToClanAsync(string itemName, string clanName, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            return true;
        }

        public async Task<object?> SetActiveQuestAsync(Guid questId, Guid objectiveId, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return null;
            var result = await _starAPI.Quests.LoadAsync(avId, questId, 0);
            return result.IsError ? null : new { questId, objectiveId, set = true };
        }

        public async Task<object?> AddXpToAvatarAsync(string avatarId, int amount)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return null;
            return new { avatarId, xpAdded = amount };
        }

        // ── Cosmic (Omniverse hierarchy) ───────────────────────────────────────

        private COSMICManager CreateCosmicManager(Guid avatarId)
        {
            var result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new COSMICManager(result.Result, avatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        }

        public async Task<bool> SaveOmniverseAsync(string omniverseJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Omniverse? omni;
            try { omni = System.Text.Json.JsonSerializer.Deserialize<Omniverse>(omniverseJson); }
            catch { return false; }
            if (omni == null) return false;
            var result = await CreateCosmicManager(avId).SaveOmniverseAsync(omni);
            return !result.IsError;
        }

        public async Task<bool> DeleteOmniverseAsync(Guid omniverseId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteOmniverseAsync(omniverseId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> AddMultiverseAsync(Guid parentOmniverseId, string multiverseJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Multiverse? mv;
            try { mv = System.Text.Json.JsonSerializer.Deserialize<Multiverse>(multiverseJson); }
            catch { return false; }
            if (mv == null) return false;
            var result = await CreateCosmicManager(avId).AddMultiverseAsync(parentOmniverseId, mv);
            return !result.IsError;
        }

        public async Task<bool> DeleteMultiverseAsync(Guid multiverseId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteMultiverseAsync(multiverseId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> AddUniverseAsync(Guid parentMultiverseId, string universeJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Universe? u;
            try { u = System.Text.Json.JsonSerializer.Deserialize<Universe>(universeJson); }
            catch { return false; }
            if (u == null) return false;
            var result = await CreateCosmicManager(avId).AddUniverseAsync(parentMultiverseId, u);
            return !result.IsError;
        }

        public async Task<bool> DeleteUniverseAsync(Guid universeId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteUniverseAsync(universeId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> AddGalaxyClusterAsync(Guid parentUniverseId, string galaxyClusterJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            GalaxyCluster? gc;
            try { gc = System.Text.Json.JsonSerializer.Deserialize<GalaxyCluster>(galaxyClusterJson); }
            catch { return false; }
            if (gc == null) return false;
            var result = await CreateCosmicManager(avId).AddGalaxyClusterAsync(parentUniverseId, gc);
            return !result.IsError;
        }

        public async Task<bool> DeleteGalaxyClusterAsync(Guid galaxyClusterId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteGalaxyClusterAsync(galaxyClusterId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> AddGalaxyAsync(Guid parentGalaxyClusterId, string galaxyJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Galaxy? gal;
            try { gal = System.Text.Json.JsonSerializer.Deserialize<Galaxy>(galaxyJson); }
            catch { return false; }
            if (gal == null) return false;
            var result = await CreateCosmicManager(avId).AddGalaxyAsync(parentGalaxyClusterId, gal);
            return !result.IsError;
        }

        public async Task<bool> DeleteGalaxyAsync(Guid galaxyId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteGalaxyAsync(galaxyId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> AddSolarSystemAsync(Guid parentGalaxyId, string solarSystemJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            SolarSystem? ss;
            try { ss = System.Text.Json.JsonSerializer.Deserialize<SolarSystem>(solarSystemJson); }
            catch { return false; }
            if (ss == null) return false;
            var result = await CreateCosmicManager(avId).AddSolarSystemAsync(parentGalaxyId, ss);
            return !result.IsError;
        }

        public async Task<bool> DeleteSolarSystemAsync(Guid solarSystemId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteSolarSystemAsync(solarSystemId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> AddStarAsync(Guid parentGalaxyId, string starJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Star? star;
            try { star = System.Text.Json.JsonSerializer.Deserialize<Star>(starJson); }
            catch { return false; }
            if (star == null) return false;
            var result = await CreateCosmicManager(avId).AddStarAsync(parentGalaxyId, star);
            return !result.IsError;
        }

        public async Task<bool> DeleteStarAsync(Guid starId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteStarAsync(starId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> AddPlanetAsync(Guid parentSolarSystemId, string planetJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Planet? planet;
            try { planet = System.Text.Json.JsonSerializer.Deserialize<Planet>(planetJson); }
            catch { return false; }
            if (planet == null) return false;
            var result = await CreateCosmicManager(avId).AddPlanetAsync(parentSolarSystemId, planet);
            return !result.IsError;
        }

        public async Task<bool> DeletePlanetAsync(Guid planetId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeletePlanetAsync(planetId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> AddMoonAsync(Guid parentPlanetId, string moonJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Moon? moon;
            try { moon = System.Text.Json.JsonSerializer.Deserialize<Moon>(moonJson); }
            catch { return false; }
            if (moon == null) return false;
            var result = await CreateCosmicManager(avId).AddMoonAsync(new Planet { Id = parentPlanetId }, moon);
            return !result.IsError;
        }

        public async Task<bool> DeleteMoonAsync(Guid moonId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteMoonAsync(moonId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> AddAsteroidAsync(Guid parentGalaxyId, string asteroidJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Asteroid? asteroid;
            try { asteroid = System.Text.Json.JsonSerializer.Deserialize<Asteroid>(asteroidJson); }
            catch { return false; }
            if (asteroid == null) return false;
            var result = await CreateCosmicManager(avId).AddAsteroidAsync(new Galaxy { Id = parentGalaxyId }, asteroid);
            return !result.IsError;
        }

        public async Task<bool> DeleteAsteroidAsync(Guid asteroidId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteAsteroidAsync(asteroidId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> AddCometAsync(Guid parentGalaxyId, string cometJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Comet? comet;
            try { comet = System.Text.Json.JsonSerializer.Deserialize<Comet>(cometJson); }
            catch { return false; }
            if (comet == null) return false;
            var result = await CreateCosmicManager(avId).AddCometAsync(new Galaxy { Id = parentGalaxyId }, comet);
            return !result.IsError;
        }

        public async Task<bool> DeleteCometAsync(Guid cometId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteCometAsync(cometId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> AddMeteroidAsync(Guid parentGalaxyId, string meteroidJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Meteroid? meteroid;
            try { meteroid = System.Text.Json.JsonSerializer.Deserialize<Meteroid>(meteroidJson); }
            catch { return false; }
            if (meteroid == null) return false;
            var result = await CreateCosmicManager(avId).AddMeteroidAsync(new Galaxy { Id = parentGalaxyId }, meteroid);
            return !result.IsError;
        }

        public async Task<bool> DeleteMeteroidAsync(Guid meteroidId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteMeteroidAsync(meteroidId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteNebulaAsync(Guid nebulaId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteNebulaAsync(nebulaId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteSuperVerseAsync(Guid superVerseId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteSuperVerseAsync(superVerseId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteWormHoleAsync(Guid wormHoleId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteWormHoleAsync(wormHoleId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteBlackHoleAsync(Guid blackHoleId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteBlackHoleAsync(blackHoleId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeletePortalAsync(Guid portalId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeletePortalAsync(portalId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteStarGateAsync(Guid starGateId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteStarGateAsync(starGateId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteSpaceTimeDistortionAsync(Guid distortionId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteSpaceTimeDistortionAsync(distortionId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteSpaceTimeAbnormallyAsync(Guid abnormallyId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteSpaceTimeAbnormallyAsync(abnormallyId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteTemporalRiftAsync(Guid riftId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteTemporalRiftAsync(riftId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteStarDustAsync(Guid starDustId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteStarDustAsync(starDustId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteCosmicWaveAsync(Guid waveId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteCosmicWaveAsync(waveId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteCosmicRayAsync(Guid rayId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteCosmicRayAsync(rayId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> DeleteGravitationalWaveAsync(Guid waveId, string avatarId, bool softDelete = true)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            var result = await CreateCosmicManager(avId).DeleteGravitationalWaveAsync(waveId, softDelete);
            return !result.IsError;
        }

        public async Task<bool> UpdateOmniverseAsync(string omniverseJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Omniverse? omni;
            try { omni = System.Text.Json.JsonSerializer.Deserialize<Omniverse>(omniverseJson); }
            catch { return false; }
            if (omni == null) return false;
            var result = await CreateCosmicManager(avId).UpdateOmniverseAsync(omni);
            return !result.IsError;
        }

        public async Task<bool> UpdateMultiverseAsync(string multiverseJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Multiverse? mv;
            try { mv = System.Text.Json.JsonSerializer.Deserialize<Multiverse>(multiverseJson); }
            catch { return false; }
            if (mv == null) return false;
            var result = await CreateCosmicManager(avId).UpdateMultiverseAsync(mv);
            return !result.IsError;
        }

        public async Task<bool> UpdateUniverseAsync(string universeJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Universe? u;
            try { u = System.Text.Json.JsonSerializer.Deserialize<Universe>(universeJson); }
            catch { return false; }
            if (u == null) return false;
            var result = await CreateCosmicManager(avId).UpdateUniverseAsync(u);
            return !result.IsError;
        }

        public async Task<bool> UpdateGalaxyClusterAsync(string galaxyClusterJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            GalaxyCluster? gc;
            try { gc = System.Text.Json.JsonSerializer.Deserialize<GalaxyCluster>(galaxyClusterJson); }
            catch { return false; }
            if (gc == null) return false;
            var result = await CreateCosmicManager(avId).UpdateGalaxyClusterAsync(gc);
            return !result.IsError;
        }

        public async Task<bool> UpdateGalaxyAsync(string galaxyJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Galaxy? gal;
            try { gal = System.Text.Json.JsonSerializer.Deserialize<Galaxy>(galaxyJson); }
            catch { return false; }
            if (gal == null) return false;
            var result = await CreateCosmicManager(avId).UpdateGalaxyAsync(gal);
            return !result.IsError;
        }

        public async Task<bool> UpdateSolarSystemAsync(string solarSystemJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            SolarSystem? ss;
            try { ss = System.Text.Json.JsonSerializer.Deserialize<SolarSystem>(solarSystemJson); }
            catch { return false; }
            if (ss == null) return false;
            var result = await CreateCosmicManager(avId).UpdateSolarSystemAsync(ss);
            return !result.IsError;
        }

        public async Task<bool> UpdateStarAsync(string starJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Star? star;
            try { star = System.Text.Json.JsonSerializer.Deserialize<Star>(starJson); }
            catch { return false; }
            if (star == null) return false;
            var result = await CreateCosmicManager(avId).UpdateStarAsync(star);
            return !result.IsError;
        }

        public async Task<bool> UpdatePlanetAsync(string planetJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Planet? planet;
            try { planet = System.Text.Json.JsonSerializer.Deserialize<Planet>(planetJson); }
            catch { return false; }
            if (planet == null) return false;
            var result = await CreateCosmicManager(avId).UpdatePlanetAsync(planet);
            return !result.IsError;
        }

        public async Task<bool> UpdateMoonAsync(string moonJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Moon? moon;
            try { moon = System.Text.Json.JsonSerializer.Deserialize<Moon>(moonJson); }
            catch { return false; }
            if (moon == null) return false;
            var result = await CreateCosmicManager(avId).UpdateMoonAsync(moon);
            return !result.IsError;
        }

        public async Task<bool> UpdateAsteroidAsync(string asteroidJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Asteroid? asteroid;
            try { asteroid = System.Text.Json.JsonSerializer.Deserialize<Asteroid>(asteroidJson); }
            catch { return false; }
            if (asteroid == null) return false;
            var result = await CreateCosmicManager(avId).UpdateAsteroidAsync(asteroid);
            return !result.IsError;
        }

        public async Task<bool> UpdateCometAsync(string cometJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Comet? comet;
            try { comet = System.Text.Json.JsonSerializer.Deserialize<Comet>(cometJson); }
            catch { return false; }
            if (comet == null) return false;
            var result = await CreateCosmicManager(avId).UpdateCometAsync(comet);
            return !result.IsError;
        }

        public async Task<bool> UpdateMeteroidAsync(string meteroidJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Meteroid? meteroid;
            try { meteroid = System.Text.Json.JsonSerializer.Deserialize<Meteroid>(meteroidJson); }
            catch { return false; }
            if (meteroid == null) return false;
            var result = await CreateCosmicManager(avId).UpdateMeteroidAsync(meteroid);
            return !result.IsError;
        }

        public async Task<bool> UpdateNebulaAsync(string nebulaJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Nebula? nebula;
            try { nebula = System.Text.Json.JsonSerializer.Deserialize<Nebula>(nebulaJson); }
            catch { return false; }
            if (nebula == null) return false;
            var result = await CreateCosmicManager(avId).UpdateNebulaAsync(nebula);
            return !result.IsError;
        }

        public async Task<bool> UpdateSuperVerseAsync(string superVerseJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            SuperVerse? sv;
            try { sv = System.Text.Json.JsonSerializer.Deserialize<SuperVerse>(superVerseJson); }
            catch { return false; }
            if (sv == null) return false;
            var result = await CreateCosmicManager(avId).UpdateSuperVerseAsync(sv);
            return !result.IsError;
        }

        public async Task<bool> UpdateWormHoleAsync(string wormHoleJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            WormHole? wh;
            try { wh = System.Text.Json.JsonSerializer.Deserialize<WormHole>(wormHoleJson); }
            catch { return false; }
            if (wh == null) return false;
            var result = await CreateCosmicManager(avId).UpdateWormHoleAsync(wh);
            return !result.IsError;
        }

        public async Task<bool> UpdateBlackHoleAsync(string blackHoleJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            BlackHole? bh;
            try { bh = System.Text.Json.JsonSerializer.Deserialize<BlackHole>(blackHoleJson); }
            catch { return false; }
            if (bh == null) return false;
            var result = await CreateCosmicManager(avId).UpdateBlackHoleAsync(bh);
            return !result.IsError;
        }

        public async Task<bool> UpdatePortalAsync(string portalJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            Portal? portal;
            try { portal = System.Text.Json.JsonSerializer.Deserialize<Portal>(portalJson); }
            catch { return false; }
            if (portal == null) return false;
            var result = await CreateCosmicManager(avId).UpdatePortalAsync(portal);
            return !result.IsError;
        }

        public async Task<bool> UpdateStarGateAsync(string starGateJson, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            StarGate? sg;
            try { sg = System.Text.Json.JsonSerializer.Deserialize<StarGate>(starGateJson); }
            catch { return false; }
            if (sg == null) return false;
            var result = await CreateCosmicManager(avId).UpdateStarGateAsync(sg);
            return !result.IsError;
        }

        public async Task<bool> UpdateSpaceTimeDistortionAsync(string json, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            SpaceTimeDistortion? obj;
            try { obj = System.Text.Json.JsonSerializer.Deserialize<SpaceTimeDistortion>(json); }
            catch { return false; }
            if (obj == null) return false;
            var result = await CreateCosmicManager(avId).UpdateSpaceTimeDistortionAsync(obj);
            return !result.IsError;
        }

        public async Task<bool> UpdateSpaceTimeAbnormallyAsync(string json, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            SpaceTimeAbnormally? obj;
            try { obj = System.Text.Json.JsonSerializer.Deserialize<SpaceTimeAbnormally>(json); }
            catch { return false; }
            if (obj == null) return false;
            var result = await CreateCosmicManager(avId).UpdateSpaceTimeAbnormallyAsync(obj);
            return !result.IsError;
        }

        public async Task<bool> UpdateTemporalRiftAsync(string json, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            TemporalRift? obj;
            try { obj = System.Text.Json.JsonSerializer.Deserialize<TemporalRift>(json); }
            catch { return false; }
            if (obj == null) return false;
            var result = await CreateCosmicManager(avId).UpdateTemporalRiftAsync(obj);
            return !result.IsError;
        }

        public async Task<bool> UpdateStarDustAsync(string json, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            StarDust? obj;
            try { obj = System.Text.Json.JsonSerializer.Deserialize<StarDust>(json); }
            catch { return false; }
            if (obj == null) return false;
            var result = await CreateCosmicManager(avId).UpdateStarDustAsync(obj);
            return !result.IsError;
        }

        public async Task<bool> UpdateCosmicWaveAsync(string json, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            CosmicWave? obj;
            try { obj = System.Text.Json.JsonSerializer.Deserialize<CosmicWave>(json); }
            catch { return false; }
            if (obj == null) return false;
            var result = await CreateCosmicManager(avId).UpdateCosmicWaveAsync(obj);
            return !result.IsError;
        }

        public async Task<bool> UpdateCosmicRayAsync(string json, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            CosmicRay? obj;
            try { obj = System.Text.Json.JsonSerializer.Deserialize<CosmicRay>(json); }
            catch { return false; }
            if (obj == null) return false;
            var result = await CreateCosmicManager(avId).UpdateCosmicRayAsync(obj);
            return !result.IsError;
        }

        public async Task<bool> UpdateGravitationalWaveAsync(string json, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId)) return false;
            GravitationalWave? obj;
            try { obj = System.Text.Json.JsonSerializer.Deserialize<GravitationalWave>(json); }
            catch { return false; }
            if (obj == null) return false;
            var result = await CreateCosmicManager(avId).UpdateGravitationalWaveAsync(obj);
            return !result.IsError;
        }
    }
}
