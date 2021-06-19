using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.STAR.CelestialContainers;

namespace NextGenSoftware.OASIS.STAR
{
    // At the centre of the Omiverse... (there can only be ONE) ;-)
    public class GreatGrandSuperStarCore : CelestialBodyCore, IGreatGrandSuperStarCore 
    {
        public IGreatGrandSuperStar GreatGrandSuperStar { get; set; }

        public GreatGrandSuperStarCore(Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {

        }

        public GreatGrandSuperStarCore(Guid id) : base(id)
        {

        }

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

        public async Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IUniverse>> result = new OASISResult<IEnumerable<IUniverse>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(GreatGrandSuperStar.ParentOmiverse.Universes, HolonType.Universe, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IUniverse>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Universe>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IUniverse>> GetAllUniversesForOmiverse(bool refresh = true)
        {
            return GetAllUniversesForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxy>> result = new OASISResult<IEnumerable<IGalaxy>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IUniverse>, IEnumerable<IGalaxy>>.CopyResult(universesResult, ref result);

            if (!universesResult.IsError)
            {
                List<IGalaxy> galaxies = new List<IGalaxy>();

                foreach (IUniverse universe in universesResult.Result)
                    galaxies.AddRange(universe.Galaxies);

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

        // Helper method to get the GrandSuperStars at the centre of each Universe.
        public async Task<OASISResult<IEnumerable<IGrandSuperStar>>> GetAllGrandSuperStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGrandSuperStar>> result = new OASISResult<IEnumerable<IGrandSuperStar>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IUniverse>, IEnumerable<IGrandSuperStar>>.CopyResult(universesResult, ref result);

            if (!universesResult.IsError)
            {
                List<IGrandSuperStar> grandSuperstars = new List<IGrandSuperStar>();

                foreach (IUniverse universe in universesResult.Result)
                    grandSuperstars.Add(universe.GrandSuperStar);

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

        public OASISResult<IEnumerable<IMoon>> GetAllMoonsForOmiverse(bool refresh = true)
        {
            return GetAllMoonsForOmiverseAsync(refresh).Result;
        }
    }
}