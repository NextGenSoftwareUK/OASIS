using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class MeteroidCore : CelestialBodyCore<Meteroid>, IMeteroidCore
    {
        public IMeteroid Meteroid { get; set; }

        public MeteroidCore(IMeteroid asteroid) : base()
        {
            this.Meteroid = asteroid;
        }

        public MeteroidCore(IMeteroid asteroid, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {
            this.Meteroid = asteroid;
        }

        public MeteroidCore(IMeteroid asteroid, Guid id) : base(id)
        {
            this.Meteroid = asteroid;
        }
    }
}