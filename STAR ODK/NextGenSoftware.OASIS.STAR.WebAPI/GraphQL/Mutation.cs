using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
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
    }
}
