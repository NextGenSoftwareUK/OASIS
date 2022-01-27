using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Meteroid : CelestialBody<Meteroid>, IMeteroid
    {
        public Meteroid() : base(HolonType.Meteroid){}

        public Meteroid(Guid id) : base(id, HolonType.Meteroid) {}

        public Meteroid(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Meteroid) { } 
    }
}