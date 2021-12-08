using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class GalaxyCluster : CelestialSpace, IGalaxyCluster
    {
        public List<IGalaxy> Galaxies { get; set; } = new List<IGalaxy>();
        public List<ISolarSystem> SoloarSystems { get; set; } = new List<ISolarSystem>(); //TODO: Can we have SoloarSystems outside of Galaxies? Think so... yes! :)
        public List<INebula> Nebulas { get; set; } = new List<INebula>();
        public List<IStar> Stars { get; set; } = new List<IStar>(); //TODO: Can we have stars outside of Galaxies? Think so... yes! :)
        public List<IPlanet> Planets { get; set; } = new List<IPlanet>(); //TODO: Can we have planets outside of Galaxies? Think so... yes! :)
        public List<IAsteroid> Asteroids { get; set; } = new List<IAsteroid>();
        public List<IComet> Comets { get; set; } = new List<IComet>();
        public List<IMeteroid> Meteroids { get; set; } = new List<IMeteroid>();

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

        public GalaxyCluster() : base(HolonType.GalaxyCluster) { }

        public GalaxyCluster(Guid id) : base(id, HolonType.GalaxyCluster) { }

        public GalaxyCluster(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.GalaxyCluster) { }
    }
}