using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Universe : CelestialSpace, IUniverse
    {
        //public List<IDimension> Dimensions { get; set; } //TODO: Think SuperStars at the centre of each Galaxy let you jump between dimensions so should dimensions be inside Galaxies? But they also let you jump to different Galaxies within that Universe so think ok how it is? :)
        private List<IGalaxyCluster> _galaxyClusters = new List<IGalaxyCluster>();
        private List<ISolarSystem> _solarSystems = new List<ISolarSystem>();
        private List<INebula> _nebulas = new List<INebula>();
        private List<IStar> _stars { get; set; } = new List<IStar>();
        private List<IPlanet> _planets { get; set; } = new List<IPlanet>();
        private List<IAsteroid> _asteroids = new List<IAsteroid>();
        private List<IComet> _comets = new List<IComet>();
        private List<IMeteroid> _meteroids { get; set; } = new List<IMeteroid>();

        public List<IGalaxyCluster> GalaxyClusters
        {
            get
            {
                return _galaxyClusters;
            }
            set
            {
                _galaxyClusters = value;
            }
        }

        //TODO: Can we have SoloarSystems outside of Galaxy Clusters? Think so... yes! :)
        public List<ISolarSystem> SolarSystems
        {
            get
            {
                return _solarSystems;
            }
            set
            {
                _solarSystems = value;
                RegisterAllCelestialSpaces();
            }
        }

        public List<INebula> Nebulas
        {
            get
            {
                return _nebulas;
            }
            set
            {
                _nebulas = value;
                RegisterAllCelestialSpaces();
            }
        }

        public List<IStar> Stars
        {
            get
            {
                return _stars;
            }
            set
            {
                _stars = value;
                RegisterAllCelestialBodies();
            }
        }

        public List<IPlanet> Planets
        {
            get
            {
                return _planets;
            }
            set
            {
                _planets = value;
                RegisterAllCelestialBodies();
            }
        }

        public List<IAsteroid> Asteroids
        {
            get
            {
                return _asteroids;
            }
            set
            {
                _asteroids = value;
                RegisterAllCelestialBodies();
            }
        }

        public List<IComet> Comets
        {
            get
            {
                return _comets;
            }
            set
            {
                _comets = value;
                RegisterAllCelestialBodies();
            }
        }

        public List<IMeteroid> Meteroids
        {
            get
            {
                return _meteroids;
            }
            set
            {
                _meteroids = value;
                RegisterAllCelestialBodies();
            }
        }

        public Universe() : base(HolonType.Universe) { }

        public Universe(Guid id) : base(id, HolonType.Universe) { }

        public Universe(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Universe) { }

        private void RegisterAllCelestialSpaces()
        {
            base.UnregisterAllCelestialSpaces();
            base.RegisterCelestialSpaces(this.SolarSystems);
            base.RegisterCelestialSpaces(this.Nebulas);
            base.RegisterCelestialSpaces(this.GalaxyClusters);
        }

        private void RegisterAllCelestialBodies()
        {
            base.UnregisterAllCelestialBodies();
            base.RegisterCelestialBodies(this.Stars);
            base.RegisterCelestialBodies(this.Planets);
            base.RegisterCelestialBodies(this.Asteroids);
            base.RegisterCelestialBodies(this.Comets);
            base.RegisterCelestialBodies(this.Meteroids);
        }
    }
}