using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class GravitationalWave : CelestialSpace, IGravitationalWave
    {
        public GravitationalWave() : base(HolonType.GalaxyCluster) { }

        public GravitationalWave(Guid id) : base(id, HolonType.GalaxyCluster) { }

        public GravitationalWave(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.GalaxyCluster) { }
    }
}