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
        public Task<OASISResult<IOmiverse>> UpdateOmniverseAsync(IOmiverse omniverse,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(omniverse, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IMultiverse>> UpdateMultiverseAsync(IMultiverse multiverse,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(multiverse, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IUniverse>> UpdateUniverseAsync(IUniverse universe,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(universe, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IGalaxyCluster>> UpdateGalaxyClusterAsync(IGalaxyCluster galaxyCluster,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(galaxyCluster, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IGalaxy>> UpdateGalaxyAsync(IGalaxy galaxy,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(galaxy, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ISolarSystem>> UpdateSolarSystemAsync(ISolarSystem solarSystem,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(solarSystem, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IStar>> UpdateStarAsync(IStar star,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(star, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IPlanet>> UpdatePlanetAsync(IPlanet planet,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(planet, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IMoon>> UpdateMoonAsync(IMoon moon,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(moon, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IAsteroid>> UpdateAsteroidAsync(IAsteroid asteroid,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(asteroid, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IComet>> UpdateCometAsync(IComet comet,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(comet, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IMeteroid>> UpdateMeteroidAsync(IMeteroid meteroid,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(meteroid, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        // Other bodies & spaces

        public Task<OASISResult<INebula>> UpdateNebulaAsync(INebula nebula,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(nebula, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ISuperVerse>> UpdateSuperVerseAsync(ISuperVerse superVerse,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(superVerse, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IWormHole>> UpdateWormHoleAsync(IWormHole wormHole,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(wormHole, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IBlackHole>> UpdateBlackHoleAsync(IBlackHole blackHole,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(blackHole, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IPortal>> UpdatePortalAsync(IPortal portal,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(portal, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IStarGate>> UpdateStarGateAsync(IStarGate starGate,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(starGate, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ISpaceTimeDistortion>> UpdateSpaceTimeDistortionAsync(ISpaceTimeDistortion distortion,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(distortion, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ISpaceTimeAbnormally>> UpdateSpaceTimeAbnormallyAsync(ISpaceTimeAbnormally abnormally,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(abnormally, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ITemporalRift>> UpdateTemporalRiftAsync(ITemporalRift rift,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(rift, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IStarDust>> UpdateStarDustAsync(IStarDust starDust,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(starDust, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ICosmicWave>> UpdateCosmicWaveAsync(ICosmicWave wave,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(wave, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ICosmicRay>> UpdateCosmicRayAsync(ICosmicRay ray,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(ray, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IGravitationalWave>> UpdateGravitationalWaveAsync(IGravitationalWave wave,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(wave, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);



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
    }
}
