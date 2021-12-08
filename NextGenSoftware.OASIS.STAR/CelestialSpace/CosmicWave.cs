using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class CosmicWave : CelestialSpace, ICosmicWave
    {
        public CosmicWave() : base(HolonType.CosmicWave) { }

        public CosmicWave(Guid id) : base(id, HolonType.CosmicWave) { }

        public CosmicWave(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.CosmicWave) { }
    }
}