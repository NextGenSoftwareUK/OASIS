using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Nebula : CelestialSpace, INebula
    {
        public Nebula() : base(HolonType.Nebula) { }

        public Nebula(Guid id, bool autoLoad = true) : base(id, HolonType.Nebula, autoLoad) { }

        //public Nebula(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Nebula) { }
        public Nebula(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.Nebula, autoLoad) { }
    }
}