using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class MoonCore : CelestialBodyCore, IMoonCore
    {
        public IMoon Moon { get; set; }

        public MoonCore(IMoon moon) : base()
        {
            this.Moon = moon;
        }

        public MoonCore(IMoon moon, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {
            this.Moon = moon;
        }

        public MoonCore(IMoon moon, Guid id) : base(id)
        {
            this.Moon = moon;
        }
    }
}