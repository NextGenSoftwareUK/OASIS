using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class SolarSystem : ISolarSystem
    {
        public IStar Star { get; set; }
        public List<IPlanet> Planets { get; set; }
    }
}