using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Galaxy : CelestialSpace, IGalaxy
    {
        private List<ISolarSystem> _solarSystems { get; set; } = new List<ISolarSystem>();
        private List<INebula> _nebulas { get; set; } = new List<INebula>();
        private List<IStar> _stars { get; set; } = new List<IStar>();
        public List<IPlanet> _planets { get; set; } = new List<IPlanet>();
        public List<IAsteroid> _asteroids { get; set; } = new List<IAsteroid>();
        public List<IComet> _comets { get; set; } = new List<IComet>();
        public List<IMeteroid> _meteroids { get; set; } = new List<IMeteroid>();

        public ISuperStar SuperStar { get; set; }
        
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

        public Galaxy() : base(HolonType.Galaxy) 
        {
            Init();
        }

        public Galaxy(Guid id, bool autoLoad = true) : base(id, HolonType.Galaxy, autoLoad) 
        {
            Init();
        }

        //public Galaxy(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Galaxy) { }
        public Galaxy(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.CosmicWave, autoLoad) 
        {
            Init();
        }

        private void Init()
        {
            if (Id == Guid.Empty)
                Id = Guid.NewGuid();

            CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);

            SuperStar = new SuperStar()
            {
                Id = Guid.NewGuid(),
                CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI),
                Name = "SuperStar",
                Description = "SuperStar at the centre of this Galaxy.",
                ParentOmniverse = this.ParentOmniverse,
                ParentOmniverseId = this.ParentOmniverseId,
                ParentMultiverse = this.ParentMultiverse,
                ParentMultiverseId = this.ParentMultiverseId,
                ParentUniverse = this.ParentUniverse,
                ParentUniverseId = this.ParentUniverseId,
                ParentDimension = this.ParentDimension,
                ParentDimensionId = this.ParentDimensionId,
                ParentGalaxyCluster = this.ParentGalaxyCluster,
                ParentGalaxyClusterId = this.ParentGalaxyClusterId,
                ParentGalaxy = this,
                ParentGalaxyId = this.Id,
                ParentCelestialSpace = this,
                ParentCelestialSpaceId = this.Id,
                ParentHolon = this,
                ParentHolonId = this.Id,
            };

            //Set it to not save/persist it because all children will be saved in one atomic batch operation when the parent (Omniverse/Multiverse) is saved.
            base.AddCelestialBody(this.SuperStar, false);

            ParentSuperStar = SuperStar;
            ParentSuperStarId = SuperStar.Id;
            ParentCelestialBody = SuperStar;
            ParentCelestialBodyId = SuperStar.Id;
            ParentHolon = SuperStar;
            ParentHolonId = SuperStar.Id;
        }

        private void RegisterAllCelestialSpaces()
        {
            base.RemoveAllCelestialSpaces(false, true, true);
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