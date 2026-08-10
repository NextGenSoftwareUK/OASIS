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



        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForSolarSystemAsync(ISolarSystem solarSystem)
        {
            var result = new OASISResult<IEnumerable<IPlanet>>();

            try
            {
                if (solarSystem?.Star?.CelestialBodyCore is IStarCore starCore)
                {
                    var planetsResult = await starCore.GetAllPlanetsForSolarSystemAsync();
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(planetsResult, result);

                    if (!planetsResult.IsError && planetsResult.Result != null)
                        result.Result = planetsResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "SolarSystem or its Star/CelestialBodyCore is null or not a valid IStarCore.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting planets for SolarSystem: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForSolarSystemAsync(Guid solarSystemId)
        {
            var loadResult = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetPlanetsForSolarSystemAsync(loadResult.Result);
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForGalaxyAsync(IGalaxy galaxy)
        {
            var result = new OASISResult<IEnumerable<ISolarSystem>>();

            try
            {
                if (galaxy?.SuperStar?.CelestialBodyCore is ISuperStarCore superCore)
                {
                    var ssResult = await superCore.GetAllSolarSystemsForGalaxyAsync();
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(ssResult, result);

                    if (!ssResult.IsError && ssResult.Result != null)
                        result.Result = ssResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "Galaxy or its SuperStar/CelestialBodyCore is null or not a valid ISuperStarCore.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting solar systems for Galaxy: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForGalaxyAsync(Guid galaxyId)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISolarSystem>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetSolarSystemsForGalaxyAsync(loadResult.Result);
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForGalaxyAsync(IGalaxy galaxy)
        {
            var result = new OASISResult<IEnumerable<IStar>>();

            try
            {
                if (galaxy?.SuperStar?.CelestialBodyCore is ISuperStarCore superCore)
                {
                    var starsResult = await superCore.GetAllStarsForGalaxyAsync();
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(starsResult, result);

                    if (!starsResult.IsError && starsResult.Result != null)
                        result.Result = starsResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "Galaxy or its SuperStar/CelestialBodyCore is null or not a valid ISuperStarCore.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting stars for Galaxy: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForGalaxyAsync(Guid galaxyId)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStar>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetStarsForGalaxyAsync(loadResult.Result);
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForGalaxyAsync(IGalaxy galaxy)
        {
            var result = new OASISResult<IEnumerable<IPlanet>>();

            try
            {
                if (galaxy?.SuperStar?.CelestialBodyCore is ISuperStarCore superCore)
                {
                    var planetsResult = await superCore.GetAllPlanetsForGalaxyAsync();
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(planetsResult, result);

                    if (!planetsResult.IsError && planetsResult.Result != null)
                        result.Result = planetsResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "Galaxy or its SuperStar/CelestialBodyCore is null or not a valid ISuperStarCore.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting planets for Galaxy: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForGalaxyAsync(Guid galaxyId)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetPlanetsForGalaxyAsync(loadResult.Result);
        }

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForGalaxyAsync(IGalaxy galaxy)
        {
            var result = new OASISResult<IEnumerable<IMoon>>();

            try
            {
                if (galaxy?.SuperStar?.CelestialBodyCore is ISuperStarCore superCore)
                {
                    var moonsResult = await superCore.GetAllMoonsForGalaxyAsync();
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(moonsResult, result);

                    if (!moonsResult.IsError && moonsResult.Result != null)
                        result.Result = moonsResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "Galaxy or its SuperStar/CelestialBodyCore is null or not a valid ISuperStarCore.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting moons for Galaxy: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForGalaxyAsync(Guid galaxyId)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetMoonsForGalaxyAsync(loadResult.Result);
        }

        public async Task<OASISResult<IEnumerable<IUniverse>>> GetUniversesForMultiverseAsync(IMultiverse multiverse)
        {
            var result = new OASISResult<IEnumerable<IUniverse>>();

            try
            {
                var universes = new List<IUniverse>();

                var third = multiverse?.Dimensions?.ThirdDimension;

                if (third?.MagicVerse != null)
                    universes.Add(third.MagicVerse);

                if (third?.ParallelUniverses != null)
                    universes.AddRange(third.ParallelUniverses);

                result.Result = universes;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting universes for Multiverse: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IUniverse>>> GetUniversesForMultiverseAsync(Guid multiverseId)
        {
            var loadResult = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IUniverse>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetUniversesForMultiverseAsync(loadResult.Result);
        }

        // Generic moon collections for all parent levels.

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForPlanetAsync(IPlanet parentPlanet)
            => GetChildrenForParentAsync<IMoon>((IHolon)parentPlanet, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForPlanetAsync(Guid planetId)
        {
            var load = await LoadTypedHolonAsync<IPlanet>(planetId, HolonType.Planet);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForPlanetAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IMoon>((IHolon)solarSystem, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IMoon>((IHolon)galaxyCluster, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IMoon>((IHolon)universe, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IMoon>((IHolon)multiverse, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IMoon>((IHolon)omniverse, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForOmniverseAsync(load.Result);
        }

        // Generic planets for all higher-level parents (beyond the core-specific helpers).

        public Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForGalaxyClusterGenericAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IPlanet>((IHolon)galaxyCluster, HolonType.Planet);

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForGalaxyClusterGenericAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPlanetsForGalaxyClusterGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IPlanet>((IHolon)universe, HolonType.Planet);

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPlanetsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForMultiverseGenericAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IPlanet>((IHolon)multiverse, HolonType.Planet);

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForMultiverseGenericAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPlanetsForMultiverseGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForOmniverseGenericAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IPlanet>((IHolon)omniverse, HolonType.Planet);

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForOmniverseGenericAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPlanetsForOmniverseGenericAsync(load.Result);
        }

        // Generic stars for all higher-level parents beyond the core-specific Galaxy helper.

        public Task<OASISResult<IEnumerable<IStar>>> GetStarsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IStar>((IHolon)galaxyCluster, HolonType.Star);

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStar>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStar>>> GetStarsForUniverseGenericAsync(IUniverse universe)
            => GetChildrenForParentAsync<IStar>((IHolon)universe, HolonType.Star);

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForUniverseGenericAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStar>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarsForUniverseGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStar>>> GetStarsForMultiverseGenericAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IStar>((IHolon)multiverse, HolonType.Star);

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForMultiverseGenericAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStar>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarsForMultiverseGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStar>>> GetStarsForOmniverseGenericAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IStar>((IHolon)omniverse, HolonType.Star);

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForOmniverseGenericAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStar>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarsForOmniverseGenericAsync(load.Result);
        }

        // Generic solar systems for all higher-level parents beyond the core-specific Galaxy helper.

        public Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForGalaxyClusterGenericAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<ISolarSystem>((IHolon)galaxyCluster, HolonType.SolarSystem);

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForGalaxyClusterGenericAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISolarSystem>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSolarSystemsForGalaxyClusterGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForUniverseGenericAsync(IUniverse universe)
            => GetChildrenForParentAsync<ISolarSystem>((IHolon)universe, HolonType.SolarSystem);

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForUniverseGenericAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISolarSystem>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSolarSystemsForUniverseGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForMultiverseGenericAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ISolarSystem>((IHolon)multiverse, HolonType.SolarSystem);

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForMultiverseGenericAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISolarSystem>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSolarSystemsForMultiverseGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForOmniverseGenericAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ISolarSystem>((IHolon)omniverse, HolonType.SolarSystem);

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForOmniverseGenericAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISolarSystem>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSolarSystemsForOmniverseGenericAsync(load.Result);
        }

        // Galaxy clusters & galaxies & universes & multiverses via generic child holon loading.

        public Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IGalaxyCluster>((IHolon)universe, HolonType.GalaxyCluster);

        public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxyCluster>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxyClustersForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IGalaxyCluster>((IHolon)multiverse, HolonType.GalaxyCluster);

        public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxyCluster>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxyClustersForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IGalaxyCluster>((IHolon)omniverse, HolonType.GalaxyCluster);

        public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxyCluster>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxyClustersForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IGalaxy>((IHolon)galaxyCluster, HolonType.Galaxy);

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxy>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxiesForGalaxyClusterAsync(load.Result);
        }

    }
}
