using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class StarGate : CelestialBody<StarGate>, IStarGate
    {
        public StarGate() : base(HolonType.StarGate){}

        public StarGate(Guid id) : base(id, HolonType.StarGate) { }

        //public StarGate(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.StarGate) {} 
        public StarGate(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.StarGate, autoLoad) { }
    }
}
