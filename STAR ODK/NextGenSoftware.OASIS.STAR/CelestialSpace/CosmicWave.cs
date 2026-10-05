using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class CosmicWave : CelestialSpace, ICosmicWave
    {
        public CosmicWave() : base(HolonType.CosmicWave) { }

        public CosmicWave(Guid id, bool autoLoad = true) : base(id, HolonType.CosmicWave, autoLoad) { }

        //public CosmicWave(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.CosmicWave) { }
        public CosmicWave(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.CosmicWave, autoLoad) { }
    }
}