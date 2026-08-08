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

    }
}