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
    // At the centre of each Galaxy
    public class SuperStarCore : CelestialBodyCore, ISuperStarCore
    {
        public ISuperStar SuperStar { get; set; }

        public SuperStarCore(Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {

        }

        public SuperStarCore(Guid id) : base(id)
        {

        }

        public async Task<OASISResult<ISolarSystem>> AddSolarSystemAsync(ISolarSystem solarSystem)
        {
            return OASISResultHolonToHolonHelper<IHolon, ISolarSystem>.CopyResult(
                await AddHolonToCollectionAsync(SuperStar, solarSystem, (List<IHolon>)Mapper<ISolarSystem, Holon>.MapBaseHolonProperties(
                    SuperStar.ParentGalaxy.SolarSystems)), new OASISResult<ISolarSystem>());
        }

        public OASISResult<ISolarSystem> AddSolarSystem(ISolarSystem solarSystem)
        {
            return AddSolarSystemAsync(solarSystem).Result;
        }

        public async Task<OASISResult<IStar>> AddStarAsync(IStar star)
        {
            return OASISResultHolonToHolonHelper<IHolon, IStar>.CopyResult(
                await AddHolonToCollectionAsync(SuperStar, star, (List<IHolon>)Mapper<IStar, Holon>.MapBaseHolonProperties(
                    SuperStar.ParentGalaxy.Stars)), new OASISResult<IStar>());
        }

        public OASISResult<IStar> AddStar(IStar star)
        {
            return AddStarAsync(star).Result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForGalaxyAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(SuperStar.ParentGalaxy.SolarSystems, HolonType.SoloarSystem, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<ISolarSystem>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, SolarSystem>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForGalaxy(bool refresh = true)
        {
            return GetAllSolarSystemsForGalaxyAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForGalaxyAsync(bool refresh = true)
        {
            //TODO: See if can make this even more efficient! ;-)
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(SuperStar.ParentGalaxy.Stars, HolonType.Star, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IStar>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, CelestialBodies.Star>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsForGalaxy(bool refresh = true)
        {
            return GetAllStarsForGalaxyAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForGalaxyAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IStar>> starsResult = await GetAllStarsForGalaxyAsync(refresh);
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

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForGalaxy(bool refresh = true)
        {
            return GetAllPlanetsForGalaxyAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForGalaxyAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IMoon>> result = new OASISResult<IEnumerable<IMoon>>();
            OASISResult<IEnumerable<IPlanet>> planetsResult = await GetAllPlanetsForGalaxyAsync(refresh);
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

        public OASISResult<IEnumerable<IMoon>> GetAllMoonsForGalaxy(bool refresh = true)
        {
            return GetAllMoonsForGalaxyAsync(refresh).Result;
        }
    }
}
