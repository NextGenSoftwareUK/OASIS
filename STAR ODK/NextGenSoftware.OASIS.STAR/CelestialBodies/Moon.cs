using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Moon : CelestialBody<Moon>, IMoon
    {
        public Moon() : base(HolonType.Moon) { }
        public Moon(Guid id, bool autoLoad = true) : base(id, HolonType.Moon, autoLoad){}
        //public Moon(Dictionary<ProviderType, string> providerKey, bool autoLoad = true) : base(providerKey, HolonType.Moon, autoLoad){} 
        public Moon(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.Moon, autoLoad) { }
    }
}
