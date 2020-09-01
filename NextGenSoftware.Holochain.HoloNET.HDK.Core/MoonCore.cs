using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class MoonCore : CelestialBodyCore, IMoonCore
    {
        private const string MOON_CORE_ZOME = "moon_core_zome"; //Name of the core zome in rust hc.
        private const string MOON_HOLON_TYPE = "moon_holon";
        private const string MOON_HOLONS_TYPE = "moon_holons";

        public MoonCore(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, MOON_CORE_ZOME, MOON_HOLON_TYPE, MOON_HOLONS_TYPE, providerKey)
        {
  
        }

        public MoonCore(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, MOON_CORE_ZOME, MOON_HOLON_TYPE, MOON_HOLONS_TYPE, providerKey)
        {

        }

        public MoonCore(HoloNETClientBase holoNETClient) : base(holoNETClient, MOON_CORE_ZOME, MOON_HOLON_TYPE, MOON_HOLONS_TYPE)
        {

        }

        public MoonCore(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type, MOON_CORE_ZOME, MOON_HOLON_TYPE, MOON_HOLONS_TYPE)
        {

        }
    }
}
