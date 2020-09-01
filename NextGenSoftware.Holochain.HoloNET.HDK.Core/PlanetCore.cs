using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class PlanetCore : CelestialBodyCore, IPlanetCore
    {
        private const string PLANET_CORE_ZOME = "planet_core_zome"; //Name of the core zome in rust hc.
        private const string PLANET_HOLON_TYPE = "planet_holon";
        private const string PLANET_HOLONS_TYPE = "planet_holons";

        public PlanetCore(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, PLANET_CORE_ZOME, PLANET_HOLON_TYPE, PLANET_HOLONS_TYPE, providerKey)
        {
  
        }

        public PlanetCore(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, PLANET_CORE_ZOME, PLANET_HOLON_TYPE, PLANET_HOLONS_TYPE, providerKey)
        {

        }

        public PlanetCore(HoloNETClientBase holoNETClient) : base(holoNETClient, PLANET_CORE_ZOME, PLANET_HOLON_TYPE, PLANET_HOLONS_TYPE)
        {

        }

        public PlanetCore(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type, PLANET_CORE_ZOME, PLANET_HOLON_TYPE, PLANET_HOLONS_TYPE)
        {

        }
    }
}
