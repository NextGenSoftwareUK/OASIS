using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Portal : CelestialBody<Portal>, IPortal
    {
        public Portal() : base(HolonType.Portal) { }

        public Portal(Guid id) : base(id, HolonType.Portal) { }
       
        public Portal(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Portal) {} 
    }
}