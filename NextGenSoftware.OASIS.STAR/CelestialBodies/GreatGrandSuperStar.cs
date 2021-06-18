using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    // At the centre of the Omiverse... (there can only be ONE) ;-)
    public class GreatGrandSuperStar : CelestialBody, IGreatGrandSuperStar
    {
        public GreatGrandSuperStar(Guid id) : base(id, GenesisType.GreatGrandSuperStar)
        {
            this.HolonType = HolonType.GreatGrandSuperStar;
        }

        public GreatGrandSuperStar(Dictionary<ProviderType, string> providerKey) : base(providerKey, GenesisType.GrandSuperStar)
        {
            this.HolonType = HolonType.GreatGrandSuperStar;
        }

        public GreatGrandSuperStar() : base(GenesisType.GreatGrandSuperStar)
        {
            this.HolonType = HolonType.GreatGrandSuperStar;
        }
    }
}