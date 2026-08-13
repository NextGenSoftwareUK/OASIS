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
    public partial class Mutation
    {

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

        // ── CelestialBodyMetaData ─────────────────────────────────────────────
        public async Task<string> CreateCelestialBodyMetaDataAsync(string name, string description, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.CelestialBodiesMetaDataDNA.CreateAsync(avId, name, description, default, string.Empty, null);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<string> UpdateCelestialBodyMetaDataAsync(Guid id, string name, string description, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var item = new NextGenSoftware.OASIS.API.ONODE.Core.Holons.CelestialBodyMetaDataDNA { Id = id, Name = name, Description = description };
            var r = await _starAPI.CelestialBodiesMetaDataDNA.UpdateAsync(avId, item);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<bool> DeleteCelestialBodyMetaDataAsync(Guid id, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.CelestialBodiesMetaDataDNA.DeleteAsync(avId, id, 0);
            return !r.IsError;
        }

        public async Task<string> CloneCelestialBodyMetaDataAsync(Guid id, string newName, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.CelestialBodiesMetaDataDNA.CloneAsync(avId, id, newName);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<string> PublishCelestialBodyMetaDataAsync(Guid id, string publishPath, bool registerOnStarnet, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.CelestialBodiesMetaDataDNA.PublishAsync(avId, publishPath, string.Empty, string.Empty, false, registerOnStarnet);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        // ── HolonMetaData ─────────────────────────────────────────────────────
        public async Task<string> CreateHolonMetaDataAsync(string name, string description, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.HolonsMetaDataDNA.CreateAsync(avId, name, description, default, string.Empty, null);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<string> UpdateHolonMetaDataAsync(Guid id, string name, string description, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var item = new NextGenSoftware.OASIS.API.ONODE.Core.Holons.HolonMetaDataDNA { Id = id, Name = name, Description = description };
            var r = await _starAPI.HolonsMetaDataDNA.UpdateAsync(avId, item);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<bool> DeleteHolonMetaDataAsync(Guid id, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.HolonsMetaDataDNA.DeleteAsync(avId, id, 0);
            return !r.IsError;
        }

        public async Task<string> CloneHolonMetaDataAsync(Guid id, string newName, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.HolonsMetaDataDNA.CloneAsync(avId, id, newName);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<string> PublishHolonMetaDataAsync(Guid id, string publishPath, bool registerOnStarnet, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.HolonsMetaDataDNA.PublishAsync(avId, publishPath, string.Empty, string.Empty, false, registerOnStarnet);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        // ── ZomeMetaData ──────────────────────────────────────────────────────
        public async Task<string> CreateZomeMetaDataAsync(string name, string description, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.ZomesMetaDataDNA.CreateAsync(avId, name, description, default, string.Empty, null);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<string> UpdateZomeMetaDataAsync(Guid id, string name, string description, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var item = new NextGenSoftware.OASIS.API.ONODE.Core.Holons.ZomeMetaDataDNA { Id = id, Name = name, Description = description };
            var r = await _starAPI.ZomesMetaDataDNA.UpdateAsync(avId, item);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<bool> DeleteZomeMetaDataAsync(Guid id, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.ZomesMetaDataDNA.DeleteAsync(avId, id, 0);
            return !r.IsError;
        }

        public async Task<string> CloneZomeMetaDataAsync(Guid id, string newName, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.ZomesMetaDataDNA.CloneAsync(avId, id, newName);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<string> PublishZomeMetaDataAsync(Guid id, string publishPath, bool registerOnStarnet, string avatarId)
        {
            Guid.TryParse(avatarId, out var avId);
            var r = await _starAPI.ZomesMetaDataDNA.PublishAsync(avId, publishPath, string.Empty, string.Empty, false, registerOnStarnet);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }
    }
}
