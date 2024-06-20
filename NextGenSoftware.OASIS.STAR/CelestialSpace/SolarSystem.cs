using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SolarSystem : CelestialSpace, ISolarSystem
    {
        private List<IAsteroid> _asteroids = new List<IAsteroid>();
        private List<IComet> _comets = new List<IComet>();
        private List<IPlanet> _planets { get; set; } = new List<IPlanet>();
        private List<IMeteroid> _meteroids { get; set; } = new List<IMeteroid>();

        public IStar Star { get; set; } = new Star() { CreatedOASISType = new API.Core.Helpers.EnumValue<OASISType>(OASISType.STARCLI) };

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

        public SolarSystem() : base(HolonType.SolarSystem) { }

        public SolarSystem(Guid id, bool autoLoad = true) : base(id, HolonType.SolarSystem, autoLoad) { }
        public SolarSystem(Guid id, IStar parentStar, bool autoLoad = true) : base(id, HolonType.SolarSystem, parentStar, autoLoad) { }
        public SolarSystem(Guid id, Guid parentStarId, bool autoLoad = true) : base(id, HolonType.SolarSystem, parentStarId, autoLoad) { }

        public SolarSystem(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.SolarSystem, autoLoad) { }
        public SolarSystem(string providerKey, ProviderType providerType, IStar parentStar, bool autoLoad = true) : base(providerKey, providerType, HolonType.SolarSystem, parentStar, autoLoad) { }
        public SolarSystem(string providerKey, ProviderType providerType, Guid parentStarId, bool autoLoad = true) : base(providerKey, providerType, HolonType.SolarSystem, parentStarId, autoLoad) { }

        private void RegisterAllCelestialBodies()
        {
            base.RemoveAllCelestialBodies(false, true, true);
            base.AddCelestialBodies(this.Planets, false);
            base.AddCelestialBodies(this.Asteroids, false);
            base.AddCelestialBodies(this.Comets, false);
            base.AddCelestialBodies(this.Meteroids, false);
        }
    }
}