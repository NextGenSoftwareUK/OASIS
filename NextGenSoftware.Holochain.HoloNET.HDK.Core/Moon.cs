using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class Moon : CelestialBody, IMoon
    {
        public Moon(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey, GenesisType.Moon)
        {

        }

        public Moon(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey, GenesisType.Moon)
        {

        }

        public Moon(HoloNETClientBase holoNETClient) : base(holoNETClient, GenesisType.Moon)
        {

        }

        public Moon(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type, GenesisType.Moon)
        {

        }
    }
}
