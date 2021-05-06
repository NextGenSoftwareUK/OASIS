using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Moon : CelestialBody, IMoon
    {
        public Moon(Dictionary<ProviderType, string> providerKey) : base(providerKey, GenesisType.Moon)
        {
            this.HolonType = HolonType.Moon;
        }

        public Moon() : base(GenesisType.Moon)
        {
            this.HolonType = HolonType.Moon;
        }

        //public Moon(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey, GenesisType.Moon)
        //{

        //}

        //public Moon(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey, GenesisType.Moon)
        //{

        //}

        //public Moon(HoloNETClientBase holoNETClient) : base(holoNETClient, GenesisType.Moon)
        //{

        //}

        //public Moon(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type, GenesisType.Moon)
        //{

        //}
    }
}
