using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class TemporalRift : CelestialSpace, ITemporalRift
    {
        public TemporalRift() : base(HolonType.TemporalRift) { }

        public TemporalRift(Guid id) : base(id, HolonType.TemporalRift) { }

        public TemporalRift(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.TemporalRift) { }
    }
}