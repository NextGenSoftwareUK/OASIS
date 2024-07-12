using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class GalaxyCluster : CelestialSpace, IGalaxyCluster
    {
        private List<IGalaxy> _galaxies { get; set; } = new List<IGalaxy>();
        private List<ISolarSystem> _solarSystems { get; set; } = new List<ISolarSystem>(); //TODO: Can we have SoloarSystems outside of Galaxies? Think so... yes! :)
        private List<INebula> _nebulas { get; set; } = new List<INebula>();
        private List<IStar> _stars { get; set; } = new List<IStar>(); //TODO: Can we have stars outside of Galaxies? Think so... yes! :)
        public List<IPlanet> _planets { get; set; } = new List<IPlanet>(); //TODO: Can we have planets outside of Galaxies? Think so... yes! :)
        public List<IAsteroid> _asteroids { get; set; } = new List<IAsteroid>();
        public List<IComet> _comets { get; set; } = new List<IComet>();
        public List<IMeteroid> _meteroids { get; set; } = new List<IMeteroid>();

        public List<IGalaxy> Galaxies
        {
            get
            {
                return _galaxies;
            }
            set
            {
                _galaxies = value;
                RegisterAllCelestialSpaces();
            }
        }

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

        public GalaxyCluster() : base(HolonType.GalaxyCluster) 
        {
            Init();
        }

        public GalaxyCluster(Guid id, bool autoLoad = true) : base(id, HolonType.GalaxyCluster, autoLoad) 
        {
            Init();
        }

        public GalaxyCluster(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.GalaxyCluster, autoLoad) 
        {
            Init();
        }

        private void Init()
        {
            if (Id == Guid.Empty)
                Id = Guid.NewGuid();

            CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
        }

        private void RegisterAllCelestialSpaces()
        {
            base.RemoveAllCelestialSpaces(false, true, true);
            base.AddCelestialSpaces(Galaxies, false);
            base.AddCelestialSpaces(SolarSystems, false);
            base.AddCelestialSpaces(Nebulas, false);
        }

        private void RegisterAllCelestialBodies()
        {
            base.RemoveAllCelestialBodies(false, true, true);
            base.AddCelestialBodies(Stars, false);
            base.AddCelestialBodies(Planets, false);
            base.AddCelestialBodies(Asteroids, false);
            base.AddCelestialBodies(Comets, false);
            base.AddCelestialBodies(Meteroids, false);
        }
    }
}