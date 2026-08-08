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


    }
}