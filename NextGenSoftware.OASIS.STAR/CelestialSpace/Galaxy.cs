using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Galaxy : Holon, IGalaxy
    {
        public ISuperStar SuperStar { get; set; } = new SuperStar();
        public List<ISolarSystem> SolarSystems { get; set; } = new List<ISolarSystem>();
        public List<INebula> Nebulas { get; set; } = new List<INebula>();
        public List<IStar> Stars { get; set; } = new List<IStar>();
        public List<IPlanet> Planets { get; set; } = new List<IPlanet>();
        public List<IAsteroid> Asteroids { get; set; } = new List<IAsteroid>();
        public List<IComet> Comets { get; set; } = new List<IComet>();
        public List<IMeteroid> Meteroids { get; set; } = new List<IMeteroid>();
    }
}