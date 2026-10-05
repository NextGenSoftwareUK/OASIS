using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    // At the centre of each Galaxy...
    public class SuperStar : Star, ISuperStar
    {
        //public SuperStar(Guid id) : base(id, HolonType.SuperStar){}
        public SuperStar(Guid id) : base(id) { }

        //public SuperStar(Dictionary<ProviderType, string> providerKey) : base(providerKey){}
        public SuperStar(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, autoLoad) { }

        public SuperStar() : base(HolonType.SuperStar){}
    }
}
