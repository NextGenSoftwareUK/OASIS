using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    // At the centre of each Universe...
    public class GrandSuperStar : Star, IGrandSuperStar
    {
        //public GrandSuperStar(Guid id) : base(id, HolonType.GrandSuperStar) { }
        public GrandSuperStar(Guid id) : base(id) { }


        //public GrandSuperStar(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.GrandSuperStar){}
        //public GrandSuperStar(Dictionary<ProviderType, string> providerKey) : base(providerKey) { }
        public GrandSuperStar(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, autoLoad) { }

        public GrandSuperStar() : base(HolonType.GrandSuperStar){ }
    }
}