using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Interfaces;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class MoonCore : CelestialBodyCore, IMoonCore
    {
        public IMoon Moon { get; set; }

        public MoonCore(IMoon moon) : base()
        {
            this.Moon = moon;
        }

        public MoonCore(Dictionary<ProviderType, string> providerKey, IMoon moon) : base(providerKey)
        {
            this.Moon = moon;
        }

        public MoonCore(Guid id, IMoon moon) : base(id)
        {
            this.Moon = moon;
        }
    }
}
