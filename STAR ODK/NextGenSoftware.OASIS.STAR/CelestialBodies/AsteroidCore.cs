using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class AsteroidCore : CelestialBodyCore<Asteroid>, IAsteroidCore
    {
        public IAsteroid Asteroid { get; set; }

        public AsteroidCore(IAsteroid asteroid) : base()
        {
            this.Asteroid = asteroid;
        }

        //public AsteroidCore(IAsteroid asteroid, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        //{
        //    this.Asteroid = asteroid;
        //}

        public AsteroidCore(IAsteroid asteroid, string providerKey, ProviderType providerType) : base(providerKey, providerType)
        {
            this.Asteroid = asteroid;
        }

        public AsteroidCore(IAsteroid asteroid, Guid id) : base(id)
        {
            this.Asteroid = asteroid;
        }
    }
}