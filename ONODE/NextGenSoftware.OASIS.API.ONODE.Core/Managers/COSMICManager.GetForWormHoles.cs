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

        // StarGates

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IStarGate>((IHolon)solarSystem, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IStarGate>((IHolon)galaxy, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IStarGate>((IHolon)galaxyCluster, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IStarGate>((IHolon)universe, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IStarGate>((IHolon)multiverse, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IStarGate>((IHolon)omniverse, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForOmniverseAsync(load.Result);
        }

        // SpaceTimeDistortions, SpaceTimeAbnormalies, TemporalRifts, StarDust, CosmicWaves, CosmicRays, GravitationalWaves
        // follow same pattern; for brevity they are grouped where they make sense (Universe/Multiverse/Omniverse).

        public Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<ISpaceTimeDistortion>((IHolon)universe, HolonType.SpaceTimeDistortion);

        public async Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeDistortion>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeDistortionsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ISpaceTimeDistortion>((IHolon)multiverse, HolonType.SpaceTimeDistortion);

        public async Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeDistortion>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeDistortionsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ISpaceTimeDistortion>((IHolon)omniverse, HolonType.SpaceTimeDistortion);

        public async Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeDistortion>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeDistortionsForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<ISpaceTimeAbnormally>((IHolon)universe, HolonType.SpaceTimeAbnormally);

        public async Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeAbnormally>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeAbnormaliesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ISpaceTimeAbnormally>((IHolon)multiverse, HolonType.SpaceTimeAbnormally);

        public async Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeAbnormally>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeAbnormaliesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ISpaceTimeAbnormally>((IHolon)omniverse, HolonType.SpaceTimeAbnormally);

        public async Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeAbnormally>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeAbnormaliesForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<ITemporalRift>((IHolon)universe, HolonType.TemporalRift);

        public async Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ITemporalRift>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetTemporalRiftsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ITemporalRift>((IHolon)multiverse, HolonType.TemporalRift);

        public async Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ITemporalRift>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetTemporalRiftsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ITemporalRift>((IHolon)omniverse, HolonType.TemporalRift);

        public async Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ITemporalRift>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetTemporalRiftsForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IStarDust>((IHolon)universe, HolonType.StarDust);

        public async Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarDust>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarDustForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IStarDust>((IHolon)multiverse, HolonType.StarDust);

        public async Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarDust>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarDustForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IStarDust>((IHolon)omniverse, HolonType.StarDust);

        public async Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarDust>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarDustForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<ICosmicWave>((IHolon)universe, HolonType.CosmicWave);

        public async Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicWavesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ICosmicWave>((IHolon)multiverse, HolonType.CosmicWave);

        public async Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicWavesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ICosmicWave>((IHolon)omniverse, HolonType.CosmicWave);

        public async Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicWavesForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<ICosmicRay>((IHolon)universe, HolonType.CosmicRay);

        public async Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicRay>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicRaysForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ICosmicRay>((IHolon)multiverse, HolonType.CosmicRay);

        public async Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicRay>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicRaysForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ICosmicRay>((IHolon)omniverse, HolonType.CosmicRay);

        public async Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicRay>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicRaysForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IGravitationalWave>((IHolon)universe, HolonType.GravitationalWave);

        public async Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGravitationalWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGravitationalWavesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IGravitationalWave>((IHolon)multiverse, HolonType.GravitationalWave);

        public async Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGravitationalWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGravitationalWavesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IGravitationalWave>((IHolon)omniverse, HolonType.GravitationalWave);

        public async Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGravitationalWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGravitationalWavesForOmniverseAsync(load.Result);
        }



        /// <summary>
        /// Creates a complete Galaxy hierarchy:
    }
}