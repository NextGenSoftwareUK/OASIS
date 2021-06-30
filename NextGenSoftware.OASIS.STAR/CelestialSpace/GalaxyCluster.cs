using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class GalaxyCluster : Holon, IGalaxyCluster
    {
        public List<IGalaxy> Galaxies { get; set; }
        public List<ISolarSystem> SoloarSystems { get; set; } //TODO: Can we have SoloarSystems outside of Galaxies? Think so... yes! :)
        public List<INebula> Nebulas { get; set; }
        public List<IStar> Stars { get; set; } //TODO: Can we have stars outside of Galaxies? Think so... yes! :)
        public List<IPlanet> Planets { get; set; } //TODO: Can we have planets outside of Galaxies? Think so... yes! :)
        public List<IAsteroid> Asteroids { get; set; }
        public List<IComet> Comets { get; set; }
        public List<IMeteroid> Meteroids { get; set; }

        public bool IsSuperCluster
        {
            get
            {
                //TODO: Need to find out the threshold when a cluster turns into a supercluster...
                if (Galaxies.Count > 10)
                    return true;
                else
                    return false;
            }
        }
    }
}