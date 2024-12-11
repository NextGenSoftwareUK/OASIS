using System;
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

        //public MeteroidCore(IMeteroid meteroid, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        //{
        //    this.Meteroid = meteroid;
        //}

        public MeteroidCore(IMeteroid meteroid, string providerKey, ProviderType providerType) : base(providerKey, providerType)
        {
            this.Meteroid = meteroid;
        }

        public MeteroidCore(IMeteroid asteroid, Guid id) : base(id)
        {
            this.Meteroid = asteroid;
        }
    }
}