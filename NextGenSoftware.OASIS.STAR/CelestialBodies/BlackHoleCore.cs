using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class BlackHoleCore : CelestialBodyCore<BlackHole>, IBlackHoleCore
    {
        public IBlackHole BlackHole { get; set; }

        public BlackHoleCore(IBlackHole blackHole) : base()
        {
            this.BlackHole = blackHole;
        }

        public BlackHoleCore(IBlackHole blackHole, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {
            this.BlackHole = blackHole;
        }

        public BlackHoleCore(IBlackHole blackHole, Guid id) : base(id)
        {
            this.BlackHole = blackHole;
        }
    }
}