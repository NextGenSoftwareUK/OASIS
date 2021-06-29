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
    // At the centre of each Multiverse...
    public class GrandSuperStarCore : CelestialBodyCore, IGrandSuperStarCore
    {
        public IGrandSuperStar GrandSuperStar { get; set; }

        public GrandSuperStarCore(IGrandSuperStar grandSuperStar) : base()
        {
            GrandSuperStar = grandSuperStar;
        }

        public GrandSuperStarCore(IGrandSuperStar grandSuperStar, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {
            GrandSuperStar = grandSuperStar;
        }

        public GrandSuperStarCore(IGrandSuperStar grandSuperStar, Guid id) : base(id)
        {
            GrandSuperStar = grandSuperStar;
        }

        public async Task<OASISResult<IUniverse>> AddUniverseAsync(IUniverse universe)
        {
            return OASISResultHolonToHolonHelper<IHolon, IUniverse>.CopyResult(
                await AddHolonToCollectionAsync(GrandSuperStar, universe, (List<IHolon>)Mapper<IUniverse, Holon>.MapBaseHolonProperties(
                    GrandSuperStar.ParentMultiverse.Universes)), new OASISResult<IUniverse>());
        }

        public OASISResult<IUniverse> AddUniverse(IUniverse universe)
        {
            return AddUniverseAsync(universe).Result;
        }

        /*
        public async Task<OASISResult<IGalaxy>> AddGalaxyAsync(IGalaxy galaxy)
        {
            return OASISResultHolonToHolonHelper<IHolon, IGalaxy>.CopyResult(
                await AddHolonToCollectionAsync(GrandSuperStar, galaxy, (List<IHolon>)Mapper<IGalaxy, Holon>.MapBaseHolonProperties(
                    GrandSuperStar.ParentUniverse.Galaxies)), new OASISResult<IGalaxy>());
        }

        public OASISResult<IGalaxy> AddGalaxy(IGalaxy solarSystem)
        {
            return AddGalaxyAsync(solarSystem).Result;
        }

        public async Task<OASISResult<IStar>> AddStarAsync(IStar star)
        {
            return OASISResultHolonToHolonHelper<IHolon, IStar>.CopyResult(
                await AddHolonToCollectionAsync(GrandSuperStar, star, (List<IHolon>)Mapper<IStar, Holon>.MapBaseHolonProperties(
                    GrandSuperStar.ParentUniverse.Stars)), new OASISResult<IStar>());
        }

        public OASISResult<IStar> AddStar(IStar star)
        {
            return AddStarAsync(star).Result;
        }*/


        public async Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IUniverse>> result = new OASISResult<IEnumerable<IUniverse>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(GrandSuperStar.ParentMultiverse.Universes, HolonType.Universe, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IUniverse>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Universe>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IUniverse>> GetAllUniversesForMultiverse(bool refresh = true)
        {
            return GetAllUniversesForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IDimension>> result = new OASISResult<IEnumerable<IDimension>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForMultiverseAsync(refresh);
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

        public OASISResult<IEnumerable<IDimension>> GetAllDimensionsForMultiverse(bool refresh = true)
        {
            return GetAllDimensionsForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxyCluster>> result = new OASISResult<IEnumerable<IGalaxyCluster>>();
            OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForMultiverseAsync(refresh);
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

        public OASISResult<IEnumerable<IGalaxyCluster>> GetAllGalaxyClustersForMultiverse(bool refresh = true)
        {
            return GetAllGalaxyClustersForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxy>> result = new OASISResult<IEnumerable<IGalaxy>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForMultiverseAsync(refresh);
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

        //public async Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForMultiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IGalaxy>> result = new OASISResult<IEnumerable<IGalaxy>>();
        //    OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(GrandSuperStar.ParentUniverse.Galaxies, HolonType.Galaxy, refresh);
        //    OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IGalaxy>>.CopyResult(holonResult, ref result);
        //    result.Result = Mapper<IHolon, Galaxy>.MapBaseHolonProperties(holonResult.Result);
        //    return result;
        //}

        public OASISResult<IEnumerable<IGalaxy>> GetAllGalaxiesForMultiverse(bool refresh = true)
        {
            return GetAllGalaxiesForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IGalaxy>> galaxiesResult = await GetAllGalaxiesForMultiverseAsync(refresh);
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

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForMultiverse(bool refresh = true)
        {
            return GetAllSolarSystemsForMultiverseAsync(refresh).Result;
        }


        // Helper method to get the SuperStars at the centre of each Galaxy.
        // TODO: I don't think we should allow SuperStars to be added to a Universe outside of a Galaxy?
        // So SuperStars MUST always be contained inside a Galaxy.
        public async Task<OASISResult<IEnumerable<ISuperStar>>> GetAllSuperStarsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISuperStar>> result = new OASISResult<IEnumerable<ISuperStar>>();
            OASISResult<IEnumerable<IGalaxy>> galaxiesResult = await GetAllGalaxiesForMultiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGalaxy>, IEnumerable<ISuperStar>>.CopyResult(galaxiesResult, ref result);

            if (!galaxiesResult.IsError)
            {
                List<ISuperStar> superstars = new List<ISuperStar>();

                foreach (IGalaxy galaxy in galaxiesResult.Result)
                    superstars.Add(galaxy.SuperStar);

                result.Result = superstars;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISuperStar>> GetAllSuperStarsForMultiverse(bool refresh = true)
        {
            return GetAllSuperStarsForMultiverseAsync(refresh).Result;
        }

        //TODO: Currently we are allowing Stars to be added outside of a Galaxy, not sure we should allow this or not?
        // In real life this is allowed so I guess ok here? :)
        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsOutSideOfGalaxiesAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(GrandSuperStar.ParentUniverse.Stars, HolonType.Star, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IStar>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, CelestialBodies.Star>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetStarsOutSideOfGalaxies(bool refresh = true)
        {
            return GetStarsOutSideOfGalaxiesAsync(refresh).Result;
        }


        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<ISuperStar>> superStarsResult = await GetAllSuperStarsForMultiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<ISuperStar>, IEnumerable<IStar>>.CopyResult(superStarsResult, ref result);
            List<IStar> stars = new List<IStar>();

            if (!superStarsResult.IsError)
            {
                foreach (ISuperStar superStar in superStarsResult.Result)
                {
                    OASISResult<IEnumerable<IStar>> starsResult = await ((ISuperStarCore)superStar.CelestialBodyCore).GetAllStarsForGalaxyAsync(refresh);

                    if (!starsResult.IsError)
                        stars.AddRange(starsResult.Result);
                }
            }

            OASISResult<IEnumerable<IStar>> starsOutsideResult = await GetStarsOutSideOfGalaxiesAsync(refresh);

            if (!starsOutsideResult.IsError)
                stars.AddRange(starsOutsideResult.Result);

            result.Result = stars;
            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsForMultiverse(bool refresh = true)
        {
            return GetAllStarsForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IStar>> starsResult = await GetAllStarsForMultiverseAsync(refresh);
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

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForMultiverse(bool refresh = true)
        {
            return GetAllPlanetsForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IMoon>> result = new OASISResult<IEnumerable<IMoon>>();
            OASISResult<IEnumerable<IPlanet>> planetsResult = await GetAllPlanetsForMultiverseAsync(refresh);
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

        public OASISResult<IEnumerable<IMoon>> GetAllMoonsForMultiverse(bool refresh = true)
        {
            return GetAllMoonsForMultiverseAsync(refresh).Result;
        }
    }
}