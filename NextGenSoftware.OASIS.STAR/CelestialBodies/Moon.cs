
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.STAR.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Moon : CelestialBody, IMoon
    {
        public Moon(string providerKey) : base(providerKey, GenesisType.Moon)
        {

        }

        public Moon() : base(GenesisType.Moon)
        {

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
