using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IGalaxyCluster : ICelestialSpace
    {
        List<IGalaxy> Galaxies { get; set; }
        List<ISolarSystem> SolarSystems { get; set; } //TODO: Can we have SoloarSystems outside of Galaxies? Think so... yes! :)
        List<IStar> Stars { get; set; } //TODO: Can we have stars outside of Galaxies? Think so... yes! :)
        List<IPlanet> Planets { get; set; } //TODO: Can we have planets outside of Galaxies? Think so... yes! :)
        List<IAsteroid> Asteroids { get; set; }
        List<IComet> Comets { get; set; }
        List<IMeteroid> Meteroids { get; set; }
        bool IsSuperCluster { get; }
    }
}