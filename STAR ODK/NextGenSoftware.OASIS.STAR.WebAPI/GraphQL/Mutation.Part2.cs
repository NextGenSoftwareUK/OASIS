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
    }
}
