using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class WormHoleCore : CelestialBodyCore, IWormHoleCore
    {
        public IWormHole WormHole { get; set; }

        public WormHoleCore(IWormHole wormHole) : base()
        {
            this.WormHole = wormHole;
        }

        public WormHoleCore(IWormHole wormHole, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {
            this.WormHole = wormHole;
        }

        public WormHoleCore(IWormHole wormHole, Guid id) : base(id)
        {
            this.WormHole = wormHole;
        }
    }
}