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

        public async Task<OASISResult<ISolarSystem>> AddSolarSystemAsync(Guid parentGalaxyId, ISolarSystem solarSystem)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(parentGalaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<ISolarSystem>();
                OASISResultHelper.CopyResult(loadResult, result);
                return result;
            }

            return await AddSolarSystemAsync(loadResult.Result, solarSystem);
        }



        public async Task<OASISResult<IStar>> AddStarAsync(IGalaxy parentGalaxy, IStar star)
        {
            var result = new OASISResult<IStar>();

            try
            {
                if (parentGalaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Galaxy cannot be null.");
                    return result;
                }

                if (star == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Star cannot be null.");
                    return result;
                }

                if (star.Id == Guid.Empty)
                {
                    star.Id = Guid.NewGuid();
                    star.IsNewHolon = true;
                }

                if (parentGalaxy.SuperStar != null &&
                    parentGalaxy.SuperStar.CelestialBodyCore is ISuperStarCore superCore)
                {
                    star.ParentGalaxy = parentGalaxy;
                    star.ParentGalaxyId = parentGalaxy.Id;
                    star.ParentHolon = parentGalaxy;
                    star.ParentHolonId = parentGalaxy.Id;
                    star.ParentCelestialSpace = parentGalaxy;
                    star.ParentCelestialSpaceId = parentGalaxy.Id;

                    var starResult = await superCore.AddStarAsync(star);
                    OASISResultHelper.CopyResult(starResult, result);
                }
                else
                {
                    var saveResult = await star.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IStar)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Star: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IStar>> AddStarAsync(Guid parentGalaxyId, IStar star)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(parentGalaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IStar>();
                OASISResultHelper.CopyResult(loadResult, result);
                return result;
            }

            return await AddStarAsync(loadResult.Result, star);
        }



        public async Task<OASISResult<IPlanet>> AddPlanetAsync(ISolarSystem parentSolarSystem, IPlanet planet)
        {
            var result = new OASISResult<IPlanet>();

            try
            {
                if (parentSolarSystem == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent SolarSystem cannot be null.");
                    return result;
                }

                if (planet == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Planet cannot be null.");
                    return result;
                }

                if (planet.Id == Guid.Empty)
                {
                    planet.Id = Guid.NewGuid();
                    planet.IsNewHolon = true;
                }

                if (parentSolarSystem.Star != null &&
                    parentSolarSystem.Star.CelestialBodyCore is IStarCore starCore)
                {
                    planet.ParentSolarSystem = parentSolarSystem;
                    planet.ParentSolarSystemId = parentSolarSystem.Id;
                    planet.ParentHolon = parentSolarSystem;
                    planet.ParentHolonId = parentSolarSystem.Id;
                    planet.ParentCelestialSpace = parentSolarSystem;
                    planet.ParentCelestialSpaceId = parentSolarSystem.Id;

                    var planetResult = await starCore.AddPlanetAsync(planet);
                    OASISResultHelper.CopyResult(planetResult, result);
                }
                else
                {
                    var saveResult = await planet.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IPlanet)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Planet: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IPlanet>> AddPlanetAsync(Guid parentSolarSystemId, IPlanet planet)
        {
            var loadResult = await LoadTypedHolonAsync<ISolarSystem>(parentSolarSystemId, HolonType.SolarSystem);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IPlanet>();
                OASISResultHelper.CopyResult(loadResult, result);
                return result;
            }

            return await AddPlanetAsync(loadResult.Result, planet);
        }



        public async Task<OASISResult<IMoon>> AddMoonAsync(IPlanet parentPlanet, IMoon moon)
        {
            var result = new OASISResult<IMoon>();

            try
            {
                if (parentPlanet == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Planet cannot be null.");
                    return result;
                }

                if (moon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Moon cannot be null.");
                    return result;
                }

                if (moon.Id == Guid.Empty)
                {
                    moon.Id = Guid.NewGuid();
                    moon.IsNewHolon = true;
                }

                var parentSolarSystem = parentPlanet.ParentSolarSystem;

                if (parentSolarSystem != null &&
                    parentSolarSystem.Star != null &&
                    parentSolarSystem.Star.CelestialBodyCore is IStarCore starCore)
                {
                    moon.ParentPlanet = parentPlanet;
                    moon.ParentPlanetId = parentPlanet.Id;
                    moon.ParentHolon = parentPlanet;
                    moon.ParentHolonId = parentPlanet.Id;

                    var moonResult = await starCore.AddMoonAsync(parentPlanet, moon);
                    OASISResultHelper.CopyResult(moonResult, result);
                }
                else
                {
                    var saveResult = await moon.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IMoon)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Moon: {ex.Message}", ex);
            }

            return result;
        }



        public async Task<OASISResult<IAsteroid>> AddAsteroidAsync(IGalaxy parentGalaxy, IAsteroid asteroid)
        {
            var result = new OASISResult<IAsteroid>();

            try
            {
                if (parentGalaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Galaxy cannot be null.");
                    return result;
                }

                if (asteroid == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Asteroid cannot be null.");
                    return result;
                }

                if (asteroid.Id == Guid.Empty)
                {
                    asteroid.Id = Guid.NewGuid();
                    asteroid.IsNewHolon = true;
                }

                if (parentGalaxy.SuperStar != null &&
                    parentGalaxy.SuperStar.CelestialBodyCore is ISuperStarCore superCore)
                {
                    asteroid.ParentGalaxy = parentGalaxy;
                    asteroid.ParentGalaxyId = parentGalaxy.Id;
                    asteroid.ParentHolon = parentGalaxy;
                    asteroid.ParentHolonId = parentGalaxy.Id;
                    asteroid.ParentCelestialSpace = parentGalaxy;
                    asteroid.ParentCelestialSpaceId = parentGalaxy.Id;

                    var asteroidResult = await superCore.AddAsteroidAsync(asteroid);
                    OASISResultHelper.CopyResult(asteroidResult, result);
                }
                else
                {
                    var saveResult = await asteroid.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IAsteroid)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Asteroid: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IComet>> AddCometAsync(IGalaxy parentGalaxy, IComet comet)
        {
            var result = new OASISResult<IComet>();

            try
            {
                if (parentGalaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Galaxy cannot be null.");
                    return result;
                }

                if (comet == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Comet cannot be null.");
                    return result;
                }

                if (comet.Id == Guid.Empty)
                {
                    comet.Id = Guid.NewGuid();
                    comet.IsNewHolon = true;
                }

                if (parentGalaxy.SuperStar != null &&
                    parentGalaxy.SuperStar.CelestialBodyCore is ISuperStarCore superCore)
                {
                    comet.ParentGalaxy = parentGalaxy;
                    comet.ParentGalaxyId = parentGalaxy.Id;
                    comet.ParentHolon = parentGalaxy;
                    comet.ParentHolonId = parentGalaxy.Id;
                    comet.ParentCelestialSpace = parentGalaxy;
                    comet.ParentCelestialSpaceId = parentGalaxy.Id;

                    var cometResult = await superCore.AddCometAsync(comet);
                    OASISResultHelper.CopyResult(cometResult, result);
                }
                else
                {
                    var saveResult = await comet.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IComet)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Comet: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IMeteroid>> AddMeteroidAsync(IGalaxy parentGalaxy, IMeteroid meteroid)
        {
            var result = new OASISResult<IMeteroid>();

            try
            {
                if (parentGalaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Galaxy cannot be null.");
                    return result;
                }

                if (meteroid == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Meteroid cannot be null.");
                    return result;
                }

                if (meteroid.Id == Guid.Empty)
                {
                    meteroid.Id = Guid.NewGuid();
                    meteroid.IsNewHolon = true;
                }

                if (parentGalaxy.SuperStar != null &&
                    parentGalaxy.SuperStar.CelestialBodyCore is ISuperStarCore superCore)
                {
                    meteroid.ParentGalaxy = parentGalaxy;
                    meteroid.ParentGalaxyId = parentGalaxy.Id;
                    meteroid.ParentHolon = parentGalaxy;
                    meteroid.ParentHolonId = parentGalaxy.Id;
                    meteroid.ParentCelestialSpace = parentGalaxy;
                    meteroid.ParentCelestialSpaceId = parentGalaxy.Id;

                    var meteroidResult = await superCore.AddMeteroidAsync(meteroid);
                    OASISResultHelper.CopyResult(meteroidResult, result);
                }
                else
                {
                    var saveResult = await meteroid.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IMeteroid)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Meteroid: {ex.Message}", ex);
            }

            return result;
        }


    }
}
