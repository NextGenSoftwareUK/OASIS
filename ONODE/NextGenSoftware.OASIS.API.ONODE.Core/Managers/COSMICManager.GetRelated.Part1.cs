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
        public Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IGalaxy>((IHolon)universe, HolonType.Galaxy);

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxy>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxiesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IGalaxy>((IHolon)multiverse, HolonType.Galaxy);

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxy>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxiesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IGalaxy>((IHolon)omniverse, HolonType.Galaxy);

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxy>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxiesForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMultiverse>>> GetMultiversesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IMultiverse>((IHolon)omniverse, HolonType.Multiverse);

        public async Task<OASISResult<IEnumerable<IMultiverse>>> GetMultiversesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMultiverse>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMultiversesForOmniverseAsync(load.Result);
        }

        // Generic asteroids, comets & meteroids for all parent types.

        // Asteroids

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)solarSystem, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)galaxy, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)galaxyCluster, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)universe, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)multiverse, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)omniverse, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForOmniverseAsync(load.Result);
        }

        // Comets

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IComet>((IHolon)solarSystem, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IComet>((IHolon)galaxy, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IComet>((IHolon)galaxyCluster, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IComet>((IHolon)universe, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IComet>((IHolon)multiverse, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IComet>((IHolon)omniverse, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForOmniverseAsync(load.Result);
        }

        // Meteroids

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)solarSystem, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)galaxy, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)galaxyCluster, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)universe, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)multiverse, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)omniverse, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForOmniverseAsync(load.Result);
        }

        // Nebulas

        public Task<OASISResult<IEnumerable<INebula>>> GetNebulasForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<INebula>((IHolon)galaxy, HolonType.Nebula);

        public async Task<OASISResult<IEnumerable<INebula>>> GetNebulasForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<INebula>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetNebulasForGalaxyAsync(load.Result);
        }
    }
}
