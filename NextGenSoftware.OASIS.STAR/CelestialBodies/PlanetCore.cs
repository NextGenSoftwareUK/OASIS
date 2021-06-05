using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Interfaces;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class PlanetCore : CelestialBodyCore, IPlanetCore
    {
        public IPlanet Planet { get; set; }

        public PlanetCore(IPlanet planet) : base()
        {
            this.Planet = planet;
        }

        public PlanetCore(Dictionary<ProviderType, string> providerKey, IPlanet planet) : base(providerKey)
        {
            this.Planet = planet;
        }

        public PlanetCore(Guid id, IPlanet planet) : base(id)
        {
            this.Planet = planet;
        }

        public async Task<OASISResult<IMoon>> AddMoonAsync(IMoon moon)
        {
            OASISResult<IMoon> result = new OASISResult<IMoon>();

            //if (moon.Id == Guid.Empty)
                //await base.SaveHolonAsync(moon);

            this.Planet.Moons.Add(moon);

            //TODO: Not sure if this method will save each holon first and then update the collection? Think it will so method above is not needed?
            OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(this.Planet.Moons);
            OASISResultHelper<IEnumerable<IHolon>, IMoon>.CopyResult(holonsResult, ref result);

            if (!result.IsError)
                result.Result = (IMoon)holonsResult.Result.FirstOrDefault(x => x.Name == moon.Name); //TODO: Need to enforce unique names within each planet.

            return result;
        }

        public async Task<OASISResult<List<IMoon>>> GetMoons()
        {
            if (this.Planet.Moons == null)
                this.Planet.Moons = (List<IMoon>)base.LoadHolonsAsync(ProviderKey, HolonType.Moon).Result;

            return this.Planet.Moons;
        }
    }
}
