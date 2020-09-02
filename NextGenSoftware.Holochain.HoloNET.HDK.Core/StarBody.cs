using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class StarBody : CelestialBody, IPlanet
    {
        //TODO: When you first create an OAPP, it needs to be a moon of the OurWorld planet, once they have raised their karma to 33 (master) 
        //then they can create a planet. The user needs to log into their avatar Star before they can create a moon/planet with the Genesis command.
        public List<IMoon> Moons { get; set; }

        //public Planet(HoloNETClientBase holoNETClient, Guid id, string providerKey) : base(holoNETClient, id, providerKey)
        //{

        //}

        //public Planet(string holochainConductorURI, HoloNETClientType type, Guid id) : base(holochainConductorURI, type, id)
        //{

        //}

        public StarBody(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey)
        {

        }

        public StarBody(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey)
        {

        }

        public StarBody(HoloNETClientBase holoNETClient) : base(holoNETClient)
        {

        }

        public StarBody(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type)
        {

        }

        //public Planet(HoloNETClientBase holoNETClient, Guid id, string providerKey) : base(holoNETClient, id, providerKey, "planet")
        //{

        //}

        //public Planet(string holochainConductorURI, HoloNETClientType type, Guid id) : base(holochainConductorURI, type, id, "planet")
        //{

        //}

        //public Planet(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey, "planet")
        //{

        //}

        //public Planet(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey, "planet")
        //{

        //}
    }
}
