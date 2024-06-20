using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SpaceTimeAbnormally : CelestialSpace, ISpaceTimeAbnormally
    {
        public SpaceTimeAbnormally() : base(HolonType.SpaceTimeAbnormally) { }

        public SpaceTimeAbnormally(Guid id, bool autoLoad = true) : base(id, HolonType.SpaceTimeAbnormally, autoLoad) { }

        public SpaceTimeAbnormally(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.SpaceTimeAbnormally, autoLoad) { }
    }
}