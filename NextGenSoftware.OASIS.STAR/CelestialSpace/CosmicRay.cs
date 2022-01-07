using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class CosmicRay : CelestialSpace, ICosmicRay
    {
        public CosmicRay() : base(HolonType.CosmicRay) { }

        public CosmicRay(Guid id) : base(id, HolonType.CosmicRay) { }

        public CosmicRay(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.CosmicRay) { }
    }
}