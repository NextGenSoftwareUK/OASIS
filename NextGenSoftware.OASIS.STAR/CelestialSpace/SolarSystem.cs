using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SolarSystem : Holon, ISolarSystem
    {
        public IStar Star { get; set; } = new CelestialBodies.Star();
        public List<IPlanet> Planets { get; set; } = new List<IPlanet>();
        public List<IAsteroid> Asteroids { get; set; } = new List<IAsteroid>();
        public List<IComet> Comets { get; set; } = new List<IComet>();
        public List<IMeteroid> Meteroids { get; set; } = new List<IMeteroid>();

        public SolarSystem()
        {
            this.HolonType = API.Core.Enums.HolonType.SolarSystem;
        }
    }
}