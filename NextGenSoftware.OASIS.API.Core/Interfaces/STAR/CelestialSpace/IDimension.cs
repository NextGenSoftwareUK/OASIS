using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IDimension : ICelestialSpace
    {
        public int DimensionLevel { get; set; }
        List<IGalaxyCluster> GalaxyClusters { get; set; }
        List<ISolarSystem> SoloarSystems { get; set; } //TODO: Can we have SoloarSystems outside of Galaxy Clusters? Think so... yes! :)
        List<IStar> Stars { get; set; } //TODO: Can we have stars outside of Galaxy Clusters? Think so... yes! :)
        List<IPlanet> Planets { get; set; } //TODO: Can we have planets outside of Galaxy Clusters? Think so... yes! :)
        List<IAsteroid> Asteroids { get; set; }
        List<IComet> Comets { get; set; }
        List<IMeteroid> Meteroids { get; set; }
    }
}