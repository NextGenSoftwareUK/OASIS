using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Universe : CelestialSpace, IUniverse
    {
        //public List<IDimension> Dimensions { get; set; } //TODO: Think SuperStars at the centre of each Galaxy let you jump between dimensions so should dimensions be inside Galaxies? But they also let you jump to different Galaxies within that Universe so think ok how it is? :)

        public List<IGalaxyCluster> GalaxyClusters { get; set; } = new List<IGalaxyCluster>();
        public List<ISolarSystem> SolarSystems { get; set; } = new List<ISolarSystem>(); //TODO: Can we have SoloarSystems outside of Galaxy Clusters? Think so... yes! :)
        public List<INebula> Nebulas { get; set; } = new List<INebula>();
        public List<IStar> Stars { get; set; } = new List<IStar>(); //TODO: Can we have stars outside of Galaxy Clusters? Think so... yes! :)
        public List<IPlanet> Planets { get; set; } = new List<IPlanet>(); //TODO: Can we have planets outside of Galaxy Clusters? Think so... yes! :)
        public List<IAsteroid> Asteroids { get; set; } = new List<IAsteroid>();
        public List<IComet> Comets { get; set; } = new List<IComet>();
        public List<IMeteroid> Meteroids { get; set; } = new List<IMeteroid>();

        public Universe() : base(HolonType.Universe) { }

        public Universe(Guid id) : base(id, HolonType.Universe) { }

        public Universe(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Universe) { }
    }
}