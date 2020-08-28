using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class Planet : PlanetBase, IPlanet
    {
        public Planet()
        {

        }

        //public Planet(HoloNETClientBase holoNETClient, Guid id) : base(holoNETClient, id)
        //{

        //}

        //public Planet(string holochainConductorURI, HoloNETClientType type, Guid id) : base(holochainConductorURI, type, id)
        //{
         
        //}

        public Planet(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey)
        {
     
        }

        public Planet(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey)
        {
    
        }
    }
}
