using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class CosmicRay : CelestialSpace, ICosmicRay
    {
        public CosmicRay() : base(HolonType.CosmicRay) { }

        public CosmicRay(Guid id, bool autoLoad = true) : base(id, HolonType.CosmicRay, autoLoad) { }

        public CosmicRay(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.CosmicRay, autoLoad) { }
    }
}