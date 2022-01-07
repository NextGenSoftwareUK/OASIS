using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Nebula : CelestialSpace, INebula
    {
        public Nebula() : base(HolonType.Nebula) { }

        public Nebula(Guid id) : base(id, HolonType.Nebula) { }

        public Nebula(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Nebula) { }
    }
}