using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SpaceTimeAbnormally : CelestialSpace, ISpaceTimeAbnormally
    {
        public SpaceTimeAbnormally() : base(HolonType.SpaceTimeAbnormally) { }

        public SpaceTimeAbnormally(Guid id, bool autoLoad = true) : base(id, HolonType.SpaceTimeAbnormally, autoLoad) { }

        //public SpaceTimeAbnormally(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.SpaceTimeAbnormally) { }
        public SpaceTimeAbnormally(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.SpaceTimeAbnormally, autoLoad) { }
    }
}