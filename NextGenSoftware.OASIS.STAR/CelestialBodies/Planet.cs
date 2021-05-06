using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Planet : CelestialBody, IPlanet
    {
        //TODO: When you first create an OAPP, it needs to be a moon of the OurWorld planet, once they have raised their karma to 33 (master) 
        //then they can create a planet. The user needs to log into their avatar Star before they can create a moon/planet with the Genesis command.
        public List<IMoon> Moons { get; set; }

        public Planet(Dictionary<ProviderType, string> providerKey) : base(providerKey, GenesisType.Planet)
        {
            this.HolonType = HolonType.Planet;
        }

        public Planet() : base(GenesisType.Planet)
        {
            this.HolonType = HolonType.Planet;
        }

        //public Planet(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey, GenesisType.Planet)
        //{

        //}

        //public Planet(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey, GenesisType.Planet)
        //{

        //}

        //public Planet(HoloNETClientBase holoNETClient) : base(holoNETClient, GenesisType.Planet)
        //{

        //}

        //public Planet(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type, GenesisType.Planet)
        //{

        //}
    }
}
