using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    // At the centre of each Solar System
    public class Star : CelestialBody, IStar
    {
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