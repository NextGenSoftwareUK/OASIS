using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Asteroid : CelestialBody, IAsteroid
    {
        public Asteroid() : base(HolonType.Asteroid)
        {
           // this.HolonType = HolonType.Asteroid;
        }

        public Asteroid(Guid id) : base(id, HolonType.Asteroid)
        {
          //  this.HolonType = HolonType.Asteroid;
        }

        public Asteroid(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Asteroid)
        {
            //this.HolonType = HolonType.Asteroid;
        } 
    }
}
