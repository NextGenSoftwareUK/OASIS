using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Universe : IUniverse
    {
        public IGrandSuperStar GrandSuperStar { get; set; }
        public List<IGalaxy> Galaxies { get; set; }

        //TODO; Technically a Universe can have SolarSystems stars & planets that do not belong to a Universe but not sure it would work within the OASIS COSMIC Object Model?
        //public List<ISolarSystem> SolarSystems { get; set; }
        //public List<IStar> Stars { get; set; }
        //public List<IPlanet> Planets { get; set; }
    }
}