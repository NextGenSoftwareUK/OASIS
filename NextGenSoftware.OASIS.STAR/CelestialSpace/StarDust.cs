using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class StarDust : CelestialSpace, IStarDust
    {
        public StarDust() : base(HolonType.StarDust) { }

        public StarDust(Guid id) : base(id, HolonType.StarDust) { }

        public StarDust(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.StarDust) { }
    }
}