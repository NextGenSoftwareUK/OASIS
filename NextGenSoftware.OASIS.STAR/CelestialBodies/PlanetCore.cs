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

        public OASISResult<IMoon> AddMoon(IMoon moon)
        {
            return AddMoonAsync(moon).Result; //TODO: Is this the best way of doing this?
        }

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IMoon>> result = new OASISResult<IEnumerable<IMoon>>();

            if (this.Planet.Moons == null || refresh)
            {
                OASISResult<IEnumerable<IHolon>> holonsResult = null;

                if (this.Id != Guid.Empty)
                     holonsResult = await base.LoadHolonsForParentAsync(Id, HolonType.Moon);
                
                else if (this.ProviderKey != null)
                    holonsResult = await base.LoadHolonsForParentAsync(ProviderKey, HolonType.Moon);
                else
                {
                    result.IsError = true;
                    result.Message = "Both Id and ProviderKey are null, one of these need to be set before calling this method.";
                }

                if (!result.IsError)
                {
                    OASISResultHelper<IEnumerable<IHolon>, IEnumerable<IMoon>>.CopyResult(holonsResult, ref result);
                    result.Result = (IEnumerable<IMoon>)holonsResult.Result; //TODO: Not sure if this cast will work? Probably not... Need to map...
                    this.Planet.Moons = result.Result.ToList();
                }
            }
            else
            {
                result.Message = "Refresh not required";
                result.Result = this.Planet.Moons;
            }

            return result;
        }

        public OASISResult<IEnumerable<IMoon>> GetMoons(bool refresh = true)
        {
            return GetMoonsAsync(refresh).Result; //TODO: Is this the best way of doing this?
        }
    }
}
