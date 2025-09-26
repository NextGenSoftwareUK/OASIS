using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IGalaxy : ICelestialSpace
    {
        ISuperStar SuperStar { get; set; }
        List<ISolarSystem> SolarSystems { get; set; }
        List<INebula> Nebulas { get; set; }
        List<IStar> Stars { get; set; }
        List<IPlanet> Planets { get; set; }
        List<IAsteroid> Asteroids { get; set; }
        List<IComet> Comets { get; set; }
        List<IMeteroid> Meteroids { get; set; }
    }
}