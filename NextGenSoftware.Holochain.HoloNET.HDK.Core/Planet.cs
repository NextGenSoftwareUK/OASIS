using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class Planet : PlanetBase, IPlanet
    {
        public Planet()
        {

        }

        public Planet(HoloNETClientBase holoNETClient, Guid id) : base(holoNETClient, id)
        {
           // Initialize(id, holoNETClient);
        }

        public Planet(string holochainConductorURI, HoloNETClientType type, Guid id) : base(holochainConductorURI, type, id)
        {
            //Initialize(id, holochainConductorURI, type);
        }

        public Planet(HoloNETClientBase holoNETClient) : base(holoNETClient)
        {
           // Initialize(holoNETClient);
        }

        public Planet(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type)
        {
           // Initialize(holochainConductorURI, type);
        }
    }
}
