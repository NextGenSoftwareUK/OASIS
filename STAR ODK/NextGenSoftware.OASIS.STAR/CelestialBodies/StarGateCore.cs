using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class StarGateCore : CelestialBodyCore<StarGate>, IStarGateCore
    {
        public IStarGate StarGate { get; set; }

        public StarGateCore(IStarGate starGate) : base()
        {
            this.StarGate = starGate;
        }

        //public StarGateCore(IStarGate starGate, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        //{
        //    this.StarGate = starGate;
        //}

        public StarGateCore(IStarGate starGate, string providerKey, ProviderType providerType) : base(providerKey, providerType)
        {
            this.StarGate = starGate;
        }

        public StarGateCore(IStarGate starGate, Guid id) : base(id)
        {
            this.StarGate = starGate;
        }
    }
}