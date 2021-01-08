using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.STAR
{
    public class StarBody : CelestialBody, IPlanet
    {
        //TODO: When you first create an OAPP, it needs to be a moon of the OurWorld planet, once they have raised their karma to 33 (master) 
        //then they can create a planet. The user needs to log into their avatar Star before they can create a moon/planet with the Genesis command.
        public List<IPlanet> Planets { get; set; }

        public StarBody(string providerKey) : base(providerKey, GenesisType.Star)
        {

        }

        public StarBody() : base(GenesisType.Star)
        {

        }


        /*
        public StarBody(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey, GenesisType.Star)
        {

        }

        public StarBody(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey, GenesisType.Star)
        {

        }

        public StarBody(HoloNETClientBase holoNETClient) : base(holoNETClient, GenesisType.Star)
        {

        }

        public StarBody(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type, GenesisType.Star)
        {

        }*/
    }
}
