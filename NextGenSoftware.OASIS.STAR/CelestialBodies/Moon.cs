using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Moon : CelestialBody<Moon>, IMoon
    {
        public Moon() : base(HolonType.Moon) { }
        public Moon(bool autoLoad = true) : base(HolonType.Moon, autoLoad){}
        public Moon(Guid id, bool autoLoad = true) : base(id, HolonType.Moon, autoLoad){}
        public Moon(Dictionary<ProviderType, string> providerKey, bool autoLoad = true) : base(providerKey, HolonType.Moon, autoLoad){} 
    }
}
