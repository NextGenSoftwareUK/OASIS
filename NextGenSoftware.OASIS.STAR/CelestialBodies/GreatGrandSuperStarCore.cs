using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.STAR.CelestialSpace;

namespace NextGenSoftware.OASIS.STAR
{
    // At the centre of the Omiverse... (there can only be ONE) ;-)
    public class GreatGrandSuperStarCore : CelestialBodyCore, IGreatGrandSuperStarCore 
    {
        public IGreatGrandSuperStar GreatGrandSuperStar { get; set; }

        public GreatGrandSuperStarCore(IGreatGrandSuperStar greatGrandSuperStar)
        {
            GreatGrandSuperStar = greatGrandSuperStar;
        }

        public GreatGrandSuperStarCore(IGreatGrandSuperStar greatGrandSuperStar, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {
            GreatGrandSuperStar = greatGrandSuperStar;
        }

        public GreatGrandSuperStarCore(IGreatGrandSuperStar greatGrandSuperStar, Guid id) : base(id)
        {
            GreatGrandSuperStar = greatGrandSuperStar;
        }

        public async Task<OASISResult<IOmiverse>> AddOmiverseAsync(IOmiverse omiverse)
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
            OASISResult<IHolon> holonResult = await SaveHolonAsync(omiverse);

            if (!holonResult.IsError && holonResult.Result != null)
                result.Result = Mapper<IHolon, Omiverse>.MapBaseHolonProperties(holonResult.Result);

            return result;

            //return OASISResultHolonToHolonHelper<IHolon, IOmiverse>.CopyResult(
            //     await SaveHolonAsync(omiverse), new OASISResult<IOmiverse>());

            //await AddHolonToCollectionAsync(GreatGrandSuperStar, omiverse, (List<IHolon>)Mapper<IOmiverse, Holon>.MapBaseHolonProperties(
            //    GreatGrandSuperStar.ParentOmiverse.Universes)), new OASISResult<IOmiverse>());
        }

        public OASISResult<IOmiverse> AddOmiverse(IOmiverse omiverse)
        {
            return AddOmiverseAsync(omiverse).Result;
        }

        /*
        public async Task<OASISResult<IUniverse>> AddUniverseAsync(IUniverse universe)
        {
            return OASISResultHolonToHolonHelper<IHolon, IUniverse>.CopyResult(
                await AddHolonToCollectionAsync(GreatGrandSuperStar, universe, (List<IHolon>)Mapper<IUniverse, Holon>.MapBaseHolonProperties(
                    GreatGrandSuperStar.ParentOmiverse.Universes)), new OASISResult<IUniverse>());
        }

        public OASISResult<IUniverse> AddUniverse(IUniverse universe)
        {
            return AddUniverseAsync(universe).Result;
        }
        */

        public async Task<OASISResult<IMultiverse>> AddMultiverseAsync(IMultiverse multiverse)
        {
            return OASISResultHolonToHolonHelper<IHolon, IMultiverse>.CopyResult(
                await AddHolonToCollectionAsync(GreatGrandSuperStar, multiverse, (List<IHolon>)Mapper<IMultiverse, Holon>.MapBaseHolonProperties(
                    GreatGrandSuperStar.ParentOmiverse.Multiverses)), new OASISResult<IMultiverse>());
        }

        public OASISResult<IMultiverse> AddMultiverse(IMultiverse multiverse)
        {
            return AddMultiverseAsync(multiverse).Result;
        }

        public async Task<OASISResult<IEnumerable<IMultiverse>>> GetAllMultiversesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IMultiverse>> result = new OASISResult<IEnumerable<IMultiverse>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(GreatGrandSuperStar.ParentOmiverse.Multiverses, HolonType.Multiverse, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IMultiverse>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Multiverse>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IMultiverse>> GetAllMultiversesForOmiverse(bool refresh = true)
        {
            return GetAllMultiversesForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IUniverse>> result = new OASISResult<IEnumerable<IUniverse>>();
            OASISResult<IEnumerable<IMultiverse>> multiversesResult = await GetAllMultiversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IMultiverse>, IEnumerable<IUniverse>>.CopyResult(multiversesResult, ref result);

            if (!multiversesResult.IsError)
            {
                List<IUniverse> universe = new List<IUniverse>();

                foreach (IMultiverse multiverse in multiversesResult.Result)
                    universe.AddRange(multiverse.Universes);

                result.Result = universe;
            }

            return result;
        }

        public OASISResult<IEnumerable<IUniverse>> GetAllUniversesForOmiverse(bool refresh = true)
        {
            return GetAllUniversesForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IDimension>> result = new OASISResult<IEnumerable<IDimension>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IUniverse>, IEnumerable<IDimension>>.CopyResult(universesResult, ref result);

            if (!universesResult.IsError)
            {
                List<IDimension> dimensions = new List<IDimension>();

                foreach (IUniverse universe in universesResult.Result)
                    dimensions.AddRange(universe.Dimensions);

                result.Result = dimensions;
            }

            return result;
        }

        public OASISResult<IEnumerable<IDimension>> GetAllDimensionsForOmiverse(bool refresh = true)
        {
            return GetAllDimensionsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxyCluster>> result = new OASISResult<IEnumerable<IGalaxyCluster>>();
            OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IDimension>, IEnumerable<IGalaxyCluster>>.CopyResult(dimensionsResult, ref result);

            if (!dimensionsResult.IsError)
            {
                List<IGalaxyCluster> clusters = new List<IGalaxyCluster>();

                foreach (IDimension dimension in dimensionsResult.Result)
                    clusters.AddRange(dimension.GalaxyClusters);

                result.Result = clusters;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGalaxyCluster>> GetAllGalaxyClustersForOmiverse(bool refresh = true)
        {
            return GetAllGalaxyClustersForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxy>> result = new OASISResult<IEnumerable<IGalaxy>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGalaxyCluster>, IEnumerable<IGalaxy>>.CopyResult(galaxyClustersResult, ref result);

            if (!galaxyClustersResult.IsError)
            {
                List<IGalaxy> galaxies = new List<IGalaxy>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    galaxies.AddRange(cluster.Galaxies);

                result.Result = galaxies;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGalaxy>> GetAllGalaxiesForOmiverse(bool refresh = true)
        {
            return GetAllGalaxiesForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IGalaxy>> galaxiesResult = await GetAllGalaxiesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGalaxy>, IEnumerable<ISolarSystem>>.CopyResult(galaxiesResult, ref result);

            if (!galaxiesResult.IsError)
            {
                List<ISolarSystem> solarSystems = new List<ISolarSystem>();

                foreach (IGalaxy galaxy in galaxiesResult.Result)
                    solarSystems.AddRange(galaxy.SolarSystems);

                result.Result = solarSystems;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForOmiverse(bool refresh = true)
        {
            return GetAllSolarSystemsForOmiverseAsync(refresh).Result;
        }

        // Helper method to get the GrandSuperStars at the centre of each Multiverse.
        public async Task<OASISResult<IEnumerable<IGrandSuperStar>>> GetAllGrandSuperStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGrandSuperStar>> result = new OASISResult<IEnumerable<IGrandSuperStar>>();
            OASISResult<IEnumerable<IMultiverse>> multiversesResult = await GetAllMultiversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IMultiverse>, IEnumerable<IGrandSuperStar>>.CopyResult(multiversesResult, ref result);

            if (!multiversesResult.IsError)
            {
                List<IGrandSuperStar> grandSuperstars = new List<IGrandSuperStar>();

                foreach (IMultiverse multiverse in multiversesResult.Result)
                    grandSuperstars.Add(multiverse.GrandSuperStar);

                result.Result = grandSuperstars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGrandSuperStar>> GetAllGrandSuperStarsForOmiverse(bool refresh = true)
        {
            return GetAllGrandSuperStarsForOmiverseAsync(refresh).Result;
        }

        // Helper method to get the SuperStars at the centre of each Galaxy.
        public async Task<OASISResult<IEnumerable<ISuperStar>>> GetAllSuperStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISuperStar>> result = new OASISResult<IEnumerable<ISuperStar>>();
            OASISResult<IEnumerable<IGrandSuperStar>> grandSuperStarsResult = await GetAllGrandSuperStarsForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGrandSuperStar>, IEnumerable<ISuperStar>>.CopyResult(grandSuperStarsResult, ref result);

            if (!grandSuperStarsResult.IsError)
            {
                List<ISuperStar> superstars = new List<ISuperStar>();

                foreach (IGrandSuperStar grandSuperStar in grandSuperStarsResult.Result)
                {
                    OASISResult<IEnumerable<ISuperStar>> superStarsResult = await ((IGrandSuperStarCore)grandSuperStar.CelestialBodyCore).GetAllSuperStarsForUniverseAsync(refresh);

                    if (!superStarsResult.IsError)
                        superstars.AddRange(superStarsResult.Result);
                }

                result.Result = superstars;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISuperStar>> GetAllSuperStarsForOmiverse(bool refresh = true)
        {
            return GetAllSuperStarsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<ISuperStar>> superStarsResult = await GetAllSuperStarsForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<ISuperStar>, IEnumerable<IStar>>.CopyResult(superStarsResult, ref result);

            if (!superStarsResult.IsError)
            {
                List<IStar> stars = new List<IStar>();

                foreach (ISuperStar superStar in superStarsResult.Result)
                {
                    OASISResult<IEnumerable<IStar>> starsResult = await ((ISuperStarCore)superStar.CelestialBodyCore).GetAllStarsForGalaxyAsync(refresh);

                    if (!starsResult.IsError)
                        stars.AddRange(starsResult.Result);
                }

                result.Result = stars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsForOmiverse(bool refresh = true)
        {
            return GetAllStarsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IStar>> starsResult = await GetAllStarsForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IStar>, IEnumerable<IPlanet>>.CopyResult(starsResult, ref result);

            if (!starsResult.IsError)
            {
                List<IPlanet> planets = new List<IPlanet>();

                foreach (IStar star in starsResult.Result)
                {
                    OASISResult<IEnumerable<IPlanet>> planetsResult = await ((IStarCore)star.CelestialBodyCore).GetAllPlanetsForSolarSystemAsync(refresh);

                    if (!planetsResult.IsError)
                        planets.AddRange(planetsResult.Result);
                }

                result.Result = planets;
            }

            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForOmiverse(bool refresh = true)
        {
            return GetAllPlanetsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IMoon>> result = new OASISResult<IEnumerable<IMoon>>();
            OASISResult<IEnumerable<IPlanet>> planetsResult = await GetAllPlanetsForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IPlanet>, IEnumerable<IMoon>>.CopyResult(planetsResult, ref result);

            if (!planetsResult.IsError)
            {
                List<IMoon> moons = new List<IMoon>();

                foreach (IPlanet planet in planetsResult.Result)
                {
                    OASISResult<IEnumerable<IMoon>> moonsResult = await ((IPlanetCore)planet.CelestialBodyCore).GetMoonsAsync(refresh);

                    if (!moonsResult.IsError)
                        moons.AddRange(moonsResult.Result);
                }

                result.Result = moons;
            }

            return result;
        }

        public OASISResult<IEnumerable<IZome>> GetAllZomesForOmiverse(bool refresh = true)
        {
            return GetAllZomesForOmiverseAsync(refresh).Result;
        }

        //TODO: Come back to this! :)
        public async Task<OASISResult<IEnumerable<IZome>>> GetAllZomesForOmiverseAsync(bool refresh = true)
        {
            List<IZome> zomes = new List<IZome>();
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IEnumerable<IStar>> starsResult = await GetAllStarsForOmiverseAsync(refresh);
            OASISResult<IEnumerable<IPlanet>> planetsResult = await GetAllPlanetsForOmiverseAsync(refresh);
            OASISResult<IEnumerable<IMoon>> moonsResult = await GetAllMoonsForOmiverseAsync(refresh);
            //OASISResultCollectionToCollectionHelper<IEnumerable<IMoon>, IEnumerable<IZome>>.CopyResult(moonsResult, ref result);

            if (!moonsResult.IsError)
            {
                foreach (IMoon moon in moonsResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await ((IMoonCore)moon.CelestialBodyCore).LoadZomesAsync();

                    if (!zomesResult.IsError)
                        zomes.AddRange(zomesResult.Result);

                    if (moon.ParentPlanet.CelestialBodyCore.Zomes != null)
                        zomes.AddRange(moon.ParentPlanet.CelestialBodyCore.Zomes);
                    else
                    {
                        OASISResult<IEnumerable<IZome>> planetZomesResult = await moon.ParentPlanet.LoadZomesAsync();

                        if (!planetZomesResult.IsError && planetZomesResult.Result != null)
                            zomes.AddRange(planetZomesResult.Result);
                    }

                    /*
                    if (moon.ParentStar.CelestialBodyCore.Zomes != null)
                        zomes.AddRange(moon.ParentStar.CelestialBodyCore.Zomes);
                    else
                    {
                        OASISResult<IEnumerable<IZome>> starZomesResult = await moon.ParentStar.LoadZomesAsync();

                        if (!starZomesResult.IsError && starZomesResult.Result != null)
                            zomes.AddRange(starZomesResult.Result);
                    }*/
                }

                result.Result = zomes;
            }

            //TODO: Think this way is better than what is commented out above?
            if (!planetsResult.IsError)
            {
                foreach (IPlanet planet in planetsResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await ((IPlanetCore)planet.CelestialBodyCore).LoadZomesAsync();

                    if (!zomesResult.IsError)
                        zomes.AddRange(zomesResult.Result);
                }
            }

            if (!starsResult.IsError)
            {
                foreach (IStar star in starsResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await ((IStarCore)star.CelestialBodyCore).LoadZomesAsync();

                    if (!zomesResult.IsError)
                        zomes.AddRange(zomesResult.Result);
                }
            }

            //TODO: Be good to get this working so it will be 4 lines of code instead of 9 for each collection! :)
            //OASISResult<IEnumerable<ICelestialBody>> celestialBodyResult = new OASISResult<IEnumerable<ICelestialBody>>();
            //OASISResultCollectionToCollectionHelper<IEnumerable<IMoon>, IEnumerable<ICelestialBody>>.CopyResult(moonsResult, ref celestialBodyResult);
            //celestialBodyResult.Result = Mapper<IMoon, CelestialBody>.MapBaseHolonProperties(moonsResult.Result);
            //zomes.AddRange(await LoadAlLZomesForCelestialBody(celestialBodyResult));

            result.Result = zomes;
            return result;
        }

        private async Task<List<IZome>> LoadAlLZomesForCelestialBody(OASISResult<IEnumerable<ICelestialBody>> celestialbodiesResult)
        {
            List<IZome> zomes = new List<IZome>();

            if (!celestialbodiesResult.IsError)
            {
                foreach (ICelestialBody celestialBody in celestialbodiesResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await celestialBody.CelestialBodyCore.LoadZomesAsync();

                    if (!zomesResult.IsError && zomesResult.Result != null)
                        zomes.AddRange(zomesResult.Result);
                }
            }

            return zomes;
        }

        public OASISResult<IEnumerable<IMoon>> GetAllMoonsForOmiverse(bool refresh = true)
        {
            return GetAllMoonsForOmiverseAsync(refresh).Result;
        }

        /*
        public async Task<OASISResult<IEnumerable<ICelestialHolon>>> GetCelestialCollection(OASISResult<IEnumerable<ICelestialHolon>> parentCollection, bool refresh = true)
        {
            OASISResult<IEnumerable<ICelestialHolon>> result = new OASISResult<IEnumerable<ICelestialHolon>>();
            //OASISResult<IEnumerable<IMultiverse>> multiversesResult = await GetAllMultiverseForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<ICelestialHolon>, IEnumerable<ICelestialHolon>>.CopyResult(parentCollection, ref result);

            if (!parentCollection.IsError)
            {
                List<ICelestialHolon> children = new List<ICelestialHolon>();

                foreach (ICelestialHolon parent in parentCollection.Result)
                    children.AddRange(parent.Universes); //TODO: Need to pass in a dyanmic property name somehow? If we can work out how to make this work we can save a lot of code with this generic method! ;-)

                result.Result = children;
            }

            return result;
        }*/
    }
}