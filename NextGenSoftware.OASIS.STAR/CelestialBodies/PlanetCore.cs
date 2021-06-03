using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Interfaces;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class PlanetCore : CelestialBodyCore, IPlanetCore
    {
       // private const string PLANET_CORE_ZOME = "planet_core_zome"; //Name of the core zome in rust hc.
       // private const string PLANET_HOLON_TYPE = "planet";
       // private const string PLANET_GET_MOONS = "planet_get_moons"; //TODO: Finish implementing (copy from StarCore).
       // private const string PLANET_ADD_MOON = "planet_add_moon";

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


        //public PlanetCore(IPlanet planet) : base(PLANET_CORE_ZOME, PLANET_HOLON_TYPE)
        //{
        //    this.Planet = planet;
        //}

        //public PlanetCore(string providerKey, IPlanet planet) : base(PLANET_CORE_ZOME, PLANET_HOLON_TYPE, providerKey)
        //{
        //    this.Planet = planet;
        //}



        /*
        public PlanetCore(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, PLANET_CORE_ZOME, PLANET_HOLON_TYPE, providerKey)
        {

        }

        public PlanetCore(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, PLANET_CORE_ZOME, PLANET_HOLON_TYPE, providerKey)
        {

        }

        public PlanetCore(HoloNETClientBase holoNETClient) : base(holoNETClient, PLANET_CORE_ZOME, PLANET_HOLON_TYPE)
        {

        }

        public PlanetCore(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type, PLANET_CORE_ZOME, PLANET_HOLON_TYPE)
        {

        }*/

        public async Task<IMoon> AddMoonAsync(IMoon moon)
        {
            //if (moon.Id == Guid.Empty)
                //await base.SaveHolonAsync(moon);

            this.Planet.Moons.Add(moon);

            //TODO: Not sure if this method will save each holon first and then update the collection? Think it will so method above is not needed?
            await base.SaveHolonsAsync(this.Planet.Moons);

            return moon;
            // return (IMoon)await base.CallZomeFunctionAsync(PLANET_ADD_MOON, moon);
        }

        public async Task<OASISResult<List<IMoon>>> GetMoons()
        {
            if (this.Planet.Moons == null)
                this.Planet.Moons = (List<IMoon>)base.LoadHolonsAsync(ProviderKey, HolonType.Moon).Result;

            return this.Planet.Moons;

            //return (List<IMoon>)await base.CallZomeFunctionAsync(PLANET_GET_MOONS, ProviderKey);
        }


        //private const string PLANET_CORE_ZOME = "planet_core_zome"; //Name of the core zome in rust hc.
        //private const string PLANET_HOLON_TYPE = "planet_holon";
        //private const string PLANET_HOLONS_TYPE = "planet_holons";

        //public PlanetCore(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, PLANET_CORE_ZOME, PLANET_HOLON_TYPE, PLANET_HOLONS_TYPE, providerKey)
        //{

        //}

        //public PlanetCore(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, PLANET_CORE_ZOME, PLANET_HOLON_TYPE, PLANET_HOLONS_TYPE, providerKey)
        //{

        //}

        //public PlanetCore(HoloNETClientBase holoNETClient) : base(holoNETClient, PLANET_CORE_ZOME, PLANET_HOLON_TYPE, PLANET_HOLONS_TYPE)
        //{

        //}

        //public PlanetCore(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type, PLANET_CORE_ZOME, PLANET_HOLON_TYPE, PLANET_HOLONS_TYPE)
        //{

        //}
    }
}
