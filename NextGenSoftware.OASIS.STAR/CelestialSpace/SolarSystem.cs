using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SolarSystem : CelestialSpace, ISolarSystem
    {
        public IStar Star { get; set; } = new CelestialBodies.Star() { CreatedOASISType = new API.Core.Helpers.EnumValue<API.Core.Enums.OASISType>(API.Core.Enums.OASISType.STARCLI) };
        public List<IPlanet> Planets { get; set; } = new List<IPlanet>();
        public List<IAsteroid> Asteroids { get; set; } = new List<IAsteroid>();
        public List<IComet> Comets { get; set; } = new List<IComet>();
        public List<IMeteroid> Meteroids { get; set; } = new List<IMeteroid>();

        public SolarSystem() : base(HolonType.SolarSystem) { }

        public SolarSystem(Guid id) : base(id, HolonType.SolarSystem) { }

        public SolarSystem(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.SolarSystem) { }
    }
}