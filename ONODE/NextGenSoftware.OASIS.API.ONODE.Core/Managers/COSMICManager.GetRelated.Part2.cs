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

        public Task<OASISResult<IEnumerable<INebula>>> GetNebulasForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<INebula>((IHolon)galaxyCluster, HolonType.Nebula);

        public async Task<OASISResult<IEnumerable<INebula>>> GetNebulasForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<INebula>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetNebulasForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<INebula>>> GetNebulasForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<INebula>((IHolon)universe, HolonType.Nebula);

        public async Task<OASISResult<IEnumerable<INebula>>> GetNebulasForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<INebula>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetNebulasForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<INebula>>> GetNebulasForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<INebula>((IHolon)multiverse, HolonType.Nebula);

        public async Task<OASISResult<IEnumerable<INebula>>> GetNebulasForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<INebula>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetNebulasForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<INebula>>> GetNebulasForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<INebula>((IHolon)omniverse, HolonType.Nebula);

        public async Task<OASISResult<IEnumerable<INebula>>> GetNebulasForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<INebula>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetNebulasForOmniverseAsync(load.Result);
        }

        // SuperVerses

        public Task<OASISResult<IEnumerable<ISuperVerse>>> GetSuperVersesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ISuperVerse>((IHolon)omniverse, HolonType.SuperVerse);

        public async Task<OASISResult<IEnumerable<ISuperVerse>>> GetSuperVersesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISuperVerse>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSuperVersesForOmniverseAsync(load.Result);
        }

        // WormHoles

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IWormHole>((IHolon)solarSystem, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IWormHole>((IHolon)galaxy, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IWormHole>((IHolon)galaxyCluster, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IWormHole>((IHolon)universe, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IWormHole>((IHolon)multiverse, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IWormHole>((IHolon)omniverse, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForOmniverseAsync(load.Result);
        }

        // BlackHoles

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)solarSystem, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)galaxy, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)galaxyCluster, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)universe, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)multiverse, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)omniverse, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForOmniverseAsync(load.Result);
        }

        // Portals

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IPortal>((IHolon)solarSystem, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IPortal>((IHolon)galaxy, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IPortal>((IHolon)galaxyCluster, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IPortal>((IHolon)universe, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IPortal>((IHolon)multiverse, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IPortal>((IHolon)omniverse, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForOmniverseAsync(load.Result);
        }
    }
}
