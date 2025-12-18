using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class COSMICManager : OASISManager
    {
        private IOmiverse _omiverse = null;

        public COSMICManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {
        }

        public COSMICManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {
        }

        public IOmiverse Omiverse
        {
            get
            {
                if (_omiverse == null)
                {
                    _omiverse = GetOmniverseAsync().ConfigureAwait(false).GetAwaiter().GetResult().Result;
                }

                return _omiverse;
            }
        }

        private async Task<OASISResult<IOmiverse>> GetOmniverseAsync()
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
            
            // Try to load existing omniverse or create new one
            // This would need to be implemented based on your STAR project structure
            // For now, returning a result that indicates omniverse needs to be created
            return result;
        }

        #region Omniverse Methods

        /// <summary>
        /// Saves the specified Omniverse instance using the underlying STAR implementation.
        /// The concrete Omniverse object must be created by the caller (in the STAR project) and
        /// passed in via the IOmiverse interface.
        /// </summary>
        public async Task<OASISResult<IOmiverse>> SaveOmniverseAsync(IOmiverse omniverse)
        {
            var result = new OASISResult<IOmiverse>();

            try
            {
                if (omniverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Omniverse cannot be null.");
                    return result;
                }

                if (omniverse.Id == Guid.Empty)
                {
                    omniverse.Id = Guid.NewGuid();
                    omniverse.IsNewHolon = true;
                }

                var saveResult = await omniverse.SaveAsync();
                OASISResultHelper.CopyResult(saveResult, result);
                result.Result = (IOmiverse)saveResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving Omniverse: {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        #region Multiverse Methods

        /// <summary>
        /// Adds a Multiverse to an existing Omniverse. The concrete Multiverse is created in the STAR project
        /// and passed in via the IMultiverse interface.
        /// </summary>
        public async Task<OASISResult<IMultiverse>> AddMultiverseAsync(IOmiverse parentOmniverse, IMultiverse multiverse)
        {
            var result = new OASISResult<IMultiverse>();

            try
            {
                if (parentOmniverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Omniverse cannot be null.");
                    return result;
                }

                if (multiverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Multiverse cannot be null.");
                    return result;
                }

                if (multiverse.Id == Guid.Empty)
                {
                    multiverse.Id = Guid.NewGuid();
                    multiverse.IsNewHolon = true;
                }

                Mapper<IOmiverse, IMultiverse>.MapParentCelestialBodyProperties(parentOmniverse, multiverse);
                multiverse.ParentOmniverse = parentOmniverse;
                multiverse.ParentOmniverseId = parentOmniverse.Id;
                multiverse.ParentHolon = parentOmniverse;
                multiverse.ParentHolonId = parentOmniverse.Id;
                multiverse.ParentCelestialSpace = parentOmniverse;
                multiverse.ParentCelestialSpaceId = parentOmniverse.Id;

                var saveResult = await multiverse.SaveAsync();
                OASISResultHelper.CopyResult(saveResult, result);
                result.Result = (IMultiverse)saveResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Multiverse: {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        #region Universe Methods

        /// <summary>
        /// Adds a Universe to an existing Multiverse using the existing STAR GrandSuperStar core API
        /// where available, otherwise falls back to saving the Universe directly.
        /// </summary>
        public async Task<OASISResult<IUniverse>> AddUniverseAsync(IMultiverse parentMultiverse, IUniverse universe)
        {
            var result = new OASISResult<IUniverse>();

            try
            {
                if (parentMultiverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Multiverse cannot be null.");
                    return result;
                }

                if (universe == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Universe cannot be null.");
                    return result;
                }

                if (universe.Id == Guid.Empty)
                {
                    universe.Id = Guid.NewGuid();
                    universe.IsNewHolon = true;
                }

                Mapper<IMultiverse, IUniverse>.MapParentCelestialBodyProperties(parentMultiverse, universe);
                universe.ParentMultiverse = parentMultiverse;
                universe.ParentMultiverseId = parentMultiverse.Id;
                universe.ParentHolon = parentMultiverse;
                universe.ParentHolonId = parentMultiverse.Id;
                universe.ParentCelestialSpace = parentMultiverse;
                universe.ParentCelestialSpaceId = parentMultiverse.Id;

                // Use GrandSuperStar core to register the Universe in the 3rd Dimension when available.
                if (parentMultiverse.GrandSuperStar != null && parentMultiverse.GrandSuperStar.CelestialBodyCore is IGrandSuperStarCore grandCore)
                {
                    var universeResult = await grandCore.AddParallelUniverseToThirdDimensionAsync(universe);
                    OASISResultHelper.CopyResult(universeResult, result);
                }
                else
                {
                    var saveResult = await universe.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IUniverse)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Universe: {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        #region GalaxyCluster Methods

        public async Task<OASISResult<IGalaxyCluster>> AddGalaxyClusterAsync(IUniverse parentUniverse, IGalaxyCluster galaxyCluster)
        {
            var result = new OASISResult<IGalaxyCluster>();

            try
            {
                if (parentUniverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Universe cannot be null.");
                    return result;
                }

                if (galaxyCluster == null)
                {
                    OASISErrorHandling.HandleError(ref result, "GalaxyCluster cannot be null.");
                    return result;
                }

                if (galaxyCluster.Id == Guid.Empty)
                {
                    galaxyCluster.Id = Guid.NewGuid();
                    galaxyCluster.IsNewHolon = true;
                }

                var parentMultiverse = parentUniverse.ParentMultiverse;

                if (parentMultiverse != null &&
                    parentMultiverse.GrandSuperStar != null &&
                    parentMultiverse.GrandSuperStar.CelestialBodyCore is IGrandSuperStarCore grandCore)
                {
                    Mapper<IMultiverse, IGalaxyCluster>.MapParentCelestialBodyProperties(parentMultiverse, galaxyCluster);
                    galaxyCluster.ParentMultiverse = parentMultiverse;
                    galaxyCluster.ParentMultiverseId = parentMultiverse.Id;
                    galaxyCluster.ParentHolon = parentMultiverse;
                    galaxyCluster.ParentHolonId = parentMultiverse.Id;
                    galaxyCluster.ParentCelestialSpace = parentMultiverse;
                    galaxyCluster.ParentCelestialSpaceId = parentMultiverse.Id;
                    galaxyCluster.ParentUniverse = parentUniverse;
                    galaxyCluster.ParentUniverseId = parentUniverse.Id;

                    var galaxyClusterResult = await grandCore.AddGalaxyClusterToUniverseAsync(parentUniverse, galaxyCluster);
                    OASISResultHelper.CopyResult(galaxyClusterResult, result);
                }
                else
                {
                    var saveResult = await galaxyCluster.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IGalaxyCluster)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding GalaxyCluster: {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        #region Galaxy Methods

        public async Task<OASISResult<IGalaxy>> AddGalaxyAsync(IGalaxyCluster parentGalaxyCluster, IGalaxy galaxy)
        {
            var result = new OASISResult<IGalaxy>();

            try
            {
                if (parentGalaxyCluster == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent GalaxyCluster cannot be null.");
                    return result;
                }

                if (galaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Galaxy cannot be null.");
                    return result;
                }

                if (galaxy.Id == Guid.Empty)
                {
                    galaxy.Id = Guid.NewGuid();
                    galaxy.IsNewHolon = true;
                }

                var parentMultiverse = parentGalaxyCluster.ParentMultiverse;

                if (parentMultiverse != null &&
                    parentMultiverse.GrandSuperStar != null &&
                    parentMultiverse.GrandSuperStar.CelestialBodyCore is IGrandSuperStarCore grandCore)
                {
                    Mapper<IGalaxyCluster, IGalaxy>.MapParentCelestialBodyProperties(parentGalaxyCluster, galaxy);
                    galaxy.ParentGalaxyCluster = parentGalaxyCluster;
                    galaxy.ParentGalaxyClusterId = parentGalaxyCluster.Id;
                    galaxy.ParentHolon = parentGalaxyCluster;
                    galaxy.ParentHolonId = parentGalaxyCluster.Id;
                    galaxy.ParentCelestialSpace = parentGalaxyCluster;
                    galaxy.ParentCelestialSpaceId = parentGalaxyCluster.Id;

                    var galaxyResult = await grandCore.AddGalaxyToGalaxyClusterAsync(parentGalaxyCluster, galaxy);
                    OASISResultHelper.CopyResult(galaxyResult, result);
                }
                else
                {
                    var saveResult = await galaxy.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IGalaxy)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Galaxy: {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        #region SolarSystem Methods

        public async Task<OASISResult<ISolarSystem>> AddSolarSystemAsync(IGalaxy parentGalaxy, ISolarSystem solarSystem)
        {
            var result = new OASISResult<ISolarSystem>();

            try
            {
                if (parentGalaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Galaxy cannot be null.");
                    return result;
                }

                if (solarSystem == null)
                {
                    OASISErrorHandling.HandleError(ref result, "SolarSystem cannot be null.");
                    return result;
                }

                if (solarSystem.Id == Guid.Empty)
                {
                    solarSystem.Id = Guid.NewGuid();
                    solarSystem.IsNewHolon = true;
                }

                // We expect the caller to have created and wired the central Star, but if they have
                // not then we still persist the SolarSystem as a CelestialSpace.
                if (parentGalaxy.SuperStar != null &&
                    parentGalaxy.SuperStar.CelestialBodyCore is ISuperStarCore superCore &&
                    solarSystem.Star != null &&
                    solarSystem.Star.CelestialBodyCore is IStarCore starCore)
                {
                    Mapper<IGalaxy, IStar>.MapParentCelestialBodyProperties(parentGalaxy, solarSystem.Star);
                    solarSystem.Star.ParentGalaxy = parentGalaxy;
                    solarSystem.Star.ParentGalaxyId = parentGalaxy.Id;
                    solarSystem.Star.ParentHolon = parentGalaxy;
                    solarSystem.Star.ParentHolonId = parentGalaxy.Id;
                    solarSystem.Star.ParentCelestialSpace = parentGalaxy;
                    solarSystem.Star.ParentCelestialSpaceId = parentGalaxy.Id;
                    solarSystem.Star.ParentSolarSystem = solarSystem;
                    solarSystem.Star.ParentSolarSystemId = solarSystem.Id;

                    var starResult = await superCore.AddStarAsync(solarSystem.Star);

                    if (!starResult.IsError && starResult.Result != null)
                    {
                        solarSystem.Star = starResult.Result;
                        Mapper<IStar, ISolarSystem>.MapParentCelestialBodyProperties(starResult.Result, solarSystem);
                        solarSystem.ParentStar = starResult.Result;
                        solarSystem.ParentStarId = starResult.Result.Id;
                        solarSystem.ParentHolon = solarSystem;
                        solarSystem.ParentHolonId = solarSystem.Id;
                        solarSystem.ParentCelestialSpace = solarSystem;
                        solarSystem.ParentCelestialSpaceId = solarSystem.Id;

                        var solarSystemResult = await starCore.AddSolarSystemAsync(solarSystem);
                        OASISResultHelper.CopyResult(solarSystemResult, result);
                    }
                    else
                    {
                        OASISResultHelper.CopyResult(starResult, result);
                    }
                }
                else
                {
                    var saveResult = await solarSystem.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (ISolarSystem)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding SolarSystem: {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        #region Star Methods

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
                    Mapper<IGalaxy, IStar>.MapParentCelestialBodyProperties(parentGalaxy, star);
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

        #endregion

        #region Planet Methods

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
                    Mapper<ISolarSystem, IPlanet>.MapParentCelestialBodyProperties(parentSolarSystem, planet);
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

        #endregion

        #region Moon Methods

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
                    Mapper<IPlanet, IMoon>.MapParentCelestialBodyProperties(parentPlanet, moon);
                    moon.ParentPlanet = parentPlanet;
                    moon.ParentPlanetId = parentPlanet.Id;
                    moon.ParentHolon = parentPlanet;
                    moon.ParentHolonId = parentPlanet.Id;
                    moon.ParentCelestialSpace = parentPlanet;
                    moon.ParentCelestialSpaceId = parentPlanet.Id;

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

        #endregion

        #region Other Celestial Bodies

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
                    Mapper<IGalaxy, IAsteroid>.MapParentCelestialBodyProperties(parentGalaxy, asteroid);
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
                    Mapper<IGalaxy, IComet>.MapParentCelestialBodyProperties(parentGalaxy, comet);
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
                    Mapper<IGalaxy, IMeteroid>.MapParentCelestialBodyProperties(parentGalaxy, meteroid);
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

        #endregion
    }
}
