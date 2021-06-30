using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISolarSystem : ICelestialSpace
    {
        IStar Star { get; set; }
        List<IPlanet> Planets { get; set; }
        List<IAsteroid> Asteroids { get; set; }
        List<IComet> Comets { get; set; }
        List<IMeteroid> Meteroids { get; set; }
    }
}