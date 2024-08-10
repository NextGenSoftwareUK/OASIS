using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class GravitationalWave : CelestialSpace, IGravitationalWave
    {
        public GravitationalWave() : base(HolonType.GalaxyCluster) { }

        public GravitationalWave(Guid id, bool autoLoad = true) : base(id, HolonType.GalaxyCluster, autoLoad) { }

        public GravitationalWave(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.GravitationalWave, autoLoad) { }
    }
}