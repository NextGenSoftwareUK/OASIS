using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class CometCore : CelestialBodyCore<Comet>, ICometCore
    {
        public IComet Comet { get; set; }

        public CometCore(IComet comet) : base()
        {
            this.Comet = comet;
        }

        public CometCore(IComet comet, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {
            this.Comet = comet;
        }

        public CometCore(IComet comet, Guid id) : base(id)
        {
            this.Comet = comet;
        }
    }
}