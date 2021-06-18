using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialContainers
{
    public class SolarSystem : Holon, ISolarSystem
    {
        public IStar Star { get; set; }
        public List<IPlanet> Planets { get; set; }
    }
}