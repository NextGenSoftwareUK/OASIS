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
    }
}
