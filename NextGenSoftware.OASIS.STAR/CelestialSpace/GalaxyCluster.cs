using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class GalaxyCluster : Holon, IGalaxyCluster
    {
        public List<IGalaxy> Galaxies { get; set; }
        public List<IStar> Stars { get; set; }

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


        //TODO; Technically a Universe can have SolarSystems stars & planets that do not belong to a Universe but not sure it would work within the OASIS COSMIC Object Model?
        //public List<ISolarSystem> SolarSystems { get; set; }
        //public List<IStar> Stars { get; set; }
        //public List<IPlanet> Planets { get; set; }
    }
}