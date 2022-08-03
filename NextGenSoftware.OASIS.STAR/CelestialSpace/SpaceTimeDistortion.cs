using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SpaceTimeDistortion : CelestialSpace, ISpaceTimeDistortion
    {
        public SpaceTimeDistortion() : base(HolonType.SpaceTimeDistortion) { }

        public SpaceTimeDistortion(Guid id, bool autoLoad = true) : base(id, HolonType.SpaceTimeDistortion, autoLoad) { }

        //public SpaceTimeDistortion(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.SpaceTimeDistortion) { }
        public SpaceTimeDistortion(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.SpaceTimeDistortion, autoLoad) { }
    }
}