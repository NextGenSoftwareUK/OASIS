using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Dimension : Holon, IDimension
    {
        //public DimensionLevel DimensionLevel { get; set; }

        /*
        public List<IGalaxyCluster> GalaxyClusters { get; set; }
        public List<ISolarSystem> SoloarSystems { get; set; } //TODO: Can we have SoloarSystems outside of Galaxy Clusters? Think so... yes! :)
        public List<INebula> Nebulas { get; set; }
        public List<IStar> Stars { get; set; } //TODO: Can we have stars outside of Galaxy Clusters? Think so... yes! :)
        public List<IPlanet> Planets { get; set; } //TODO: Can we have planets outside of Galaxy Clusters? Think so... yes! :)
        public List<IAsteroid> Asteroids { get; set; }
        public List<IComet> Comets { get; set; }
        public List<IMeteroid> Meteroids { get; set; }
        */

        public Dimension()
        {
            this.HolonType = HolonType.Dimension;
        }
    }
}