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


        public Star(HolonType holonType) : base(holonType) { }


        //public Star(Guid id, HolonType holonType) : base(id, holonType)
        //{
        //    //this.HolonType = HolonType.Star;
        //}

        public Star(Guid id) : base(id, HolonType.Star) { }

        //public Star(Dictionary<ProviderType, string> providerKey, HolonType holonType) : base(providerKey, holonType)
        //{
        //    //this.HolonType = HolonType.Star;
        //}

        public Star(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Star) { }


        public Star() : base(HolonType.Star) { }

        //public Star(Guid id) : base(id, HolonType.Star)
        //{
        //    //this.HolonType = HolonType.Star;
        //}

        //public Star(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Star)
        //{
        //    //this.HolonType = HolonType.Star;
        //}  
    }
}