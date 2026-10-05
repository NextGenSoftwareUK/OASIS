using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class MoonCore : CelestialBodyCore<Moon>, IMoonCore
    {
        public IMoon Moon { get; set; }

        public MoonCore(IMoon moon) : base()
        {
            this.Moon = moon;
        }

        //public MoonCore(IMoon moon, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        //{
        //    this.Moon = moon;
        //}

        public MoonCore(IMoon moon, string providerKey, ProviderType providerType) : base(providerKey, providerType)
        {
            this.Moon = moon;
        }

        public MoonCore(IMoon moon, Guid id) : base(id)
        {
            this.Moon = moon;
        }
    }
}