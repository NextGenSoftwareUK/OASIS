using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;

namespace NextGenSoftware.OASIS.API.STAR
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
