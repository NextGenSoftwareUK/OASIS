using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class StarGate : CelestialBody, IStarGate
    {
        public StarGate() : base(HolonType.StarGate){}

        public StarGate(Guid id) : base(id, HolonType.StarGate) { }

        public StarGate(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.StarGate) {} 
    }
}
