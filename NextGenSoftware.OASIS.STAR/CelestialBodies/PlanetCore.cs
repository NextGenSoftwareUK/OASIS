using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class PlanetCore : CelestialBodyCore, IPlanetCore
    {
        public IPlanet Planet { get; set; }

        public PlanetCore(IPlanet planet) : base()
        {
            this.Planet = planet;
        }

        public PlanetCore(IPlanet planet, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {
            this.Planet = planet;
        }

        public PlanetCore(IPlanet planet, Guid id) : base(id)
        {
            this.Planet = planet;
        }

        //public async Task<OASISResult<IMoon>> AddMoonAsync(IMoon moon)
        //{
        //    return OASISResultHolonToHolonHelper<IHolon, IMoon>.CopyResult(
        //        await AddHolonToCollectionAsync(Planet, moon, (List<IHolon>)Mapper<IMoon, Holon>.MapBaseHolonProperties(
        //            Planet.Moons)), new OASISResult<IMoon>());
        //}

        //public OASISResult<IMoon> AddMoon(IMoon moon)
        //{
        //    return AddMoonAsync(moon).Result; //TODO: Is this the best way of doing this?
        //}

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IMoon>> result = new OASISResult<IEnumerable<IMoon>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(Planet.Moons, HolonType.Moon, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IMoon>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Moon>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IMoon>> GetMoons(bool refresh = true)
        {
            return GetMoonsAsync(refresh).Result; //TODO: Is this the best way of doing this?
        }
    }
}
