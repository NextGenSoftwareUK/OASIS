using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class WormHole : CelestialBody, IWormHole
    {
        public WormHole() : base(HolonType.WormHole){}

        public WormHole(Guid id) : base(id, HolonType.WormHole) {}

        public WormHole(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.WormHole) {} 
    }
}
