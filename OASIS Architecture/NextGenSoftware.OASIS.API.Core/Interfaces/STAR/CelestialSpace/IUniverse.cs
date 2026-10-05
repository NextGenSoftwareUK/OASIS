using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IUniverse : ICelestialSpace
    {
        //List<IDimension> Dimensions { get; set; } //TODO: Think SuperStars at the centre of each Galaxy let you jump between dimensions so should dimensions be inside Galaxies? But they also let you jump to different Galaxies within that Universe so think ok how it is? :)

        public List<IGalaxyCluster> GalaxyClusters { get; set; }
        public List<ISolarSystem> SolarSystems { get; set; } //TODO: Can we have SolarSystems outside of Galaxy Clusters? Think so... yes! :)
        public List<INebula> Nebulas { get; set; }
        public List<IStar> Stars { get; set; } //TODO: Can we have stars outside of Galaxy Clusters? Think so... yes! :)
        public List<IPlanet> Planets { get; set; } //TODO: Can we have planets outside of Galaxy Clusters? Think so... yes! :)
        public List<IAsteroid> Asteroids { get; set; }
        public List<IComet> Comets { get; set; }
        public List<IMeteroid> Meteroids { get; set; }
    }
}