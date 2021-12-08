using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SpaceTimeDistortion : CelestialSpace, ISpaceTimeDistortion
    {
        public SpaceTimeDistortion() : base(HolonType.SpaceTimeDistortion) { }

        public SpaceTimeDistortion(Guid id) : base(id, HolonType.SpaceTimeDistortion) { }

        public SpaceTimeDistortion(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.SpaceTimeDistortion) { }
    }
}