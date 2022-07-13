using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    // At the centre of each Solar System
    public class Star : CelestialBody<Star>, IStar
    {
        public int Luminosity { get; set; }
        public StarType StarType { get; set; }
        public StarClassification StarClassification { get; set; }
        public StarBinaryType StarBinaryType { get; set; }

        public Star(HolonType holonType, bool autoLoad = true) : base(holonType, autoLoad) { }
        public Star(Guid id, bool autoLoad = true) : base(id, HolonType.Star, autoLoad) { }
        public Star(Dictionary<ProviderType, string> providerKey, bool autoLoad = true) : base(providerKey, HolonType.Star, autoLoad) { }
        public Star(bool autoLoad = true) : base(HolonType.Star, autoLoad) { }
        public Star() : base(HolonType.Star) { }
    }
}