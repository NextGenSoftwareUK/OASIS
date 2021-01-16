using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.OASIS.STAR
{
    public class MoonCore : CelestialBodyCore, IMoonCore
    {
        public IMoon Moon { get; set; }

        public MoonCore(IMoon moon) : base()
        {
            this.Moon = moon;
        }

        public MoonCore(string providerKey, IMoon moon) : base(providerKey)
        {
            this.Moon = moon;
        }

        /*
        private const string MOON_CORE_ZOME = "moon_core_zome"; //Name of the core zome in rust hc.
        private const string MOON_HOLON_TYPE = "moon";

        public MoonCore(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, MOON_CORE_ZOME, MOON_HOLON_TYPE, providerKey)
        {
  
        }

        public MoonCore(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, MOON_CORE_ZOME, MOON_HOLON_TYPE, providerKey)
        {

        }

        public MoonCore(HoloNETClientBase holoNETClient) : base(holoNETClient, MOON_CORE_ZOME, MOON_HOLON_TYPE)
        {

        }

        public MoonCore(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type, MOON_CORE_ZOME, MOON_HOLON_TYPE)
        {

        }*/
    }
}
