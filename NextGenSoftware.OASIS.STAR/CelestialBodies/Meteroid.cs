using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Meteroid : CelestialBody, IMeteroid
    {
        public Meteroid() : base(HolonType.Meteroid)
        {
            this.HolonType = HolonType.Meteroid;
        }

        public Meteroid(Guid id) : base(id, HolonType.Meteroid)
        {
            this.HolonType = HolonType.Meteroid;
        }

        public Meteroid(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Meteroid)
        {
            this.HolonType = HolonType.Meteroid;
        } 
    }
}
