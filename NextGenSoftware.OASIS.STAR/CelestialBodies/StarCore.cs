using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    // At the centre of each Solar System (creates Solar Systems, Planets & Moons).
    public class StarCore : CelestialBodyCore, IStarCore
    {
        public IStar Star { get; set; }

        public StarCore(IStar star) : base()
        {
            this.Star = star;
        }

        public StarCore(IStar star, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {
            this.Star = star;
        }

        public StarCore(IStar star, Guid id) : base(id)
        {
            this.Star = star;
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

        public async Task<OASISResult<IPlanet>> AddPlanetAsync(IPlanet planet)
        {
            return OASISResultHolonToHolonHelper<IHolon, IPlanet>.CopyResult(
                await AddHolonToCollectionAsync(Star, planet, (List<IHolon>)Mapper<IPlanet, Holon>.MapBaseHolonProperties(
                    Star.ParentSolarSystem.Planets)), new OASISResult<IPlanet>());
        }

        public OASISResult<IPlanet> AddPlanet(IPlanet planet)
        {
            return AddPlanetAsync(planet).Result;
        }

        public async Task<OASISResult<IMoon>> AddMoonAsync(IMoon moon)
        {
            return OASISResultHolonToHolonHelper<IHolon, IMoon>.CopyResult(
                await AddHolonToCollectionAsync(Planet, moon, (List<IHolon>)Mapper<IMoon, Holon>.MapBaseHolonProperties(
                    Planet.Moons)), new OASISResult<IMoon>());
        }

        public OASISResult<IMoon> AddMoon(IMoon moon)
        {
            return AddMoonAsync(moon).Result; //TODO: Is this the best way of doing this?
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForSolarSystemAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(Star.ParentSolarSystem.Planets, HolonType.Planet, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IPlanet>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Planet>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForSolarSystem(bool refresh = true)
        {
            return GetAllPlanetsForSolarSystemAsync(refresh).Result;
        }  
    }
}
