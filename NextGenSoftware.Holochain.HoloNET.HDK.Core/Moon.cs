using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class Moon : CelestialBody, IMoon
    {
        public Moon()
        {

        }

        //public Planet(HoloNETClientBase holoNETClient, Guid id) : base(holoNETClient, id)
        //{

        //}

        //public Planet(string holochainConductorURI, HoloNETClientType type, Guid id) : base(holochainConductorURI, type, id)
        //{
         
        //}

        public Moon(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey)
        {
     
        }

        public Moon(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey)
        {
    
        }
    }
}
