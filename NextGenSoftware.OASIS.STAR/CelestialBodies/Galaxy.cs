using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Galaxy : IGalaxy
    {
        public ISuperStar SuperStar { get; set; }
        public List<ISolarSystem> SolarSystems { get; set; }
        
        //TODO; Technically a Galaxy can have stars and planets that do not belong to a SolarSystem but not sure it would work within the OASIS COSMIC Object Model?
        //public List<IStar> Stars { get; set; }
        //public List<IPlanet> Planets { get; set; }
    }
}