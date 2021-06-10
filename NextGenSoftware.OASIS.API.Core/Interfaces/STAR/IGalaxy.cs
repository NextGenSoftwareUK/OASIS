using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IGalaxy
    {
        ISuperStar SuperStar { get; set; }
        List<ISolarSystem> SolarSystems { get; set; }

        //TODO; Technically a Galaxy can have stars and planets that do not belong to a SolarSystem but not sure it would work within the OASIS COSMIC Object Model?
        //List<IStar> Stars { get; set; }
        //List<IPlanet> Planets { get; set; }
    }
}