using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class TemporalRift : CelestialSpace, ITemporalRift
    {
        public TemporalRift() : base(HolonType.TemporalRift) { }

        public TemporalRift(Guid id, bool autoLoad = true) : base(id, HolonType.TemporalRift, autoLoad) { }

        public TemporalRift(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.TemporalRift, autoLoad) { }
    }
}