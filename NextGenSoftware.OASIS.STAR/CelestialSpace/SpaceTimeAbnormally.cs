using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SpaceTimeAbnormally : CelestialSpace, ISpaceTimeAbnormally
    {
        public SpaceTimeAbnormally() : base(HolonType.SpaceTimeAbnormally) { }

        public SpaceTimeAbnormally(Guid id) : base(id, HolonType.SpaceTimeAbnormally) { }

        public SpaceTimeAbnormally(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.SpaceTimeAbnormally) { }
    }
}