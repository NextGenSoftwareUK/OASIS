using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Moon : CelestialBody, IMoon
    {
        public Moon() : base(HolonType.Moon)
        {
            this.HolonType = HolonType.Moon;
        }

        public Moon(Guid id) : base(id, HolonType.Moon)
        {
            this.HolonType = HolonType.Moon;
        }

        public Moon(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Moon)
        {
            this.HolonType = HolonType.Moon;
        } 
    }
}
