using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Comet : CelestialBody, IComet
    {
        public Comet() : base(HolonType.Comet)
        {
           // this.HolonType = HolonType.Comet;
        }

        public Comet(Guid id) : base(id, HolonType.Comet)
        {
           // this.HolonType = HolonType.Comet;
        }

        public Comet(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Comet)
        {
          //  this.HolonType = HolonType.Comet;
        } 
    }
}
