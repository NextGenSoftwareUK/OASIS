using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class COSMICManager
    {

        public Task<OASISResult<bool>> DeleteOmniverseAsync(IOmiverse omniverse,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(omniverse, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteOmniverseAsync(Guid omniverseId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteMultiverseAsync(IMultiverse multiverse,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(multiverse, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteMultiverseAsync(Guid multiverseId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteUniverseAsync(IUniverse universe,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(universe, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteUniverseAsync(Guid universeId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteGalaxyClusterAsync(IGalaxyCluster galaxyCluster,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(galaxyCluster, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteGalaxyClusterAsync(Guid galaxyClusterId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteGalaxyAsync(IGalaxy galaxy,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(galaxy, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteGalaxyAsync(Guid galaxyId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteSolarSystemAsync(ISolarSystem solarSystem,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(solarSystem, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteSolarSystemAsync(Guid solarSystemId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteStarAsync(IStar star,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(star, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteStarAsync(Guid starId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IStar>(starId, HolonType.Star);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeletePlanetAsync(IPlanet planet,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(planet, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeletePlanetAsync(Guid planetId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IPlanet>(planetId, HolonType.Planet);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteMoonAsync(IMoon moon,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(moon, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteMoonAsync(Guid moonId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IMoon>(moonId, HolonType.Moon);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteAsteroidAsync(IAsteroid asteroid,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(asteroid, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteAsteroidAsync(Guid asteroidId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IAsteroid>(asteroidId, HolonType.Asteroid);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteCometAsync(IComet comet,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(comet, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteCometAsync(Guid cometId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IComet>(cometId, HolonType.Comet);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteMeteroidAsync(IMeteroid meteroid,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(meteroid, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteMeteroidAsync(Guid meteroidId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IMeteroid>(meteroidId, HolonType.Meteroid);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        // Other bodies & spaces

        public Task<OASISResult<bool>> DeleteNebulaAsync(INebula nebula,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(nebula, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteNebulaAsync(Guid nebulaId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<INebula>(nebulaId, HolonType.Nebula);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteSuperVerseAsync(ISuperVerse superVerse,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(superVerse, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteSuperVerseAsync(Guid superVerseId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ISuperVerse>(superVerseId, HolonType.SuperVerse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteWormHoleAsync(IWormHole wormHole,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(wormHole, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteWormHoleAsync(Guid wormHoleId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IWormHole>(wormHoleId, HolonType.WormHole);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteBlackHoleAsync(IBlackHole blackHole,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(blackHole, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteBlackHoleAsync(Guid blackHoleId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IBlackHole>(blackHoleId, HolonType.BlackHole);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeletePortalAsync(IPortal portal,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(portal, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeletePortalAsync(Guid portalId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IPortal>(portalId, HolonType.Portal);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteStarGateAsync(IStarGate starGate,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(starGate, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteStarGateAsync(Guid starGateId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IStarGate>(starGateId, HolonType.StarGate);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteSpaceTimeDistortionAsync(ISpaceTimeDistortion distortion,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(distortion, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteSpaceTimeDistortionAsync(Guid distortionId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ISpaceTimeDistortion>(distortionId, HolonType.SpaceTimeDistortion);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteSpaceTimeAbnormallyAsync(ISpaceTimeAbnormally abnormally,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(abnormally, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteSpaceTimeAbnormallyAsync(Guid abnormallyId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ISpaceTimeAbnormally>(abnormallyId, HolonType.SpaceTimeAbnormally);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteTemporalRiftAsync(ITemporalRift rift,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(rift, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteTemporalRiftAsync(Guid riftId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ITemporalRift>(riftId, HolonType.TemporalRift);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteStarDustAsync(IStarDust starDust,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(starDust, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteStarDustAsync(Guid starDustId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IStarDust>(starDustId, HolonType.StarDust);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteCosmicWaveAsync(ICosmicWave wave,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(wave, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteCosmicWaveAsync(Guid waveId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ICosmicWave>(waveId, HolonType.CosmicWave);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteCosmicRayAsync(ICosmicRay ray,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(ray, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteCosmicRayAsync(Guid rayId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ICosmicRay>(rayId, HolonType.CosmicRay);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteGravitationalWaveAsync(IGravitationalWave wave,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(wave, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteGravitationalWaveAsync(Guid waveId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IGravitationalWave>(waveId, HolonType.GravitationalWave);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }


    }
}