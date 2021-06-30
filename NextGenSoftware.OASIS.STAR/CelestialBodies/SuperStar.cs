using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    // At the centre of each Galaxy...
    public class SuperStar : CelestialBody, ISuperStar
    {
        public SuperStar(Guid id) : base(id, HolonType.SuperStar)
        {
            this.HolonType = HolonType.SuperStar;
        }

        public SuperStar(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.SuperStar)
        {
            this.HolonType = HolonType.SuperStar;
        }

        public SuperStar() : base(HolonType.SuperStar)
        {
            this.HolonType = HolonType.SuperStar;
        }
    }
}
