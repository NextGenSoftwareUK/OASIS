using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.STAR
{
    public class PlanetCore : CelestialBodyCore, IPlanetCore
    {
        private const string PLANET_CORE_ZOME = "planet_core_zome"; //Name of the core zome in rust hc.
        private const string PLANET_HOLON_TYPE = "planet";
        private const string PLANET_GET_MOONS = "planet_get_moons"; //TODO: Finish implementing (copy from StarCore).
        private const string PLANET_ADD_MOON = "planet_add_moon";

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

        }

        public async Task<IMoon> AddMoonAsync(IMoon moon)
        {
            return (IMoon)await base.CallZomeFunctionAsync(PLANET_ADD_MOON, moon);
        }

        public async Task<List<IMoon>> GetMoons()
        {
            if (string.IsNullOrEmpty(ProviderKey))
                throw new System.ArgumentException("ERROR: ProviderKey is null, please set this before calling this method.", "ProviderKey");

            return (List<IMoon>)await base.CallZomeFunctionAsync(PLANET_GET_MOONS, ProviderKey);
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
