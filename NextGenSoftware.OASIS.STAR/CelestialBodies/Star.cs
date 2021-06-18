using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Star : CelestialBody, IStar
    {
        //TODO: When you first create an OAPP, it needs to be a moon of the OurWorld planet, once they have raised their karma to 33 (master) 
        //then they can create a planet. The user needs to log into their avatar Star before they can create a moon/planet with the Genesis command.
       // public List<IPlanet> Planets { get; set; }

        public Star(Guid id) : base(id, GenesisType.Star)
        {
            this.HolonType = HolonType.Star;
        }

        public Star(Dictionary<ProviderType, string> providerKey) : base(providerKey, GenesisType.Star)
        {
            this.HolonType = HolonType.Star;
        }

        public Star() : base(GenesisType.Star)
        {
            this.HolonType = HolonType.Star;
        }
    }
}
