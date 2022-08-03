using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SolarSystem : CelestialSpace, ISolarSystem
    {
        private List<IAsteroid> _asteroids = new List<IAsteroid>();
        private List<IComet> _comets = new List<IComet>();
        private List<IPlanet> _planets { get; set; } = new List<IPlanet>();
        private List<IMeteroid> _meteroids { get; set; } = new List<IMeteroid>();

        public IStar Star { get; set; } = new CelestialBodies.Star() { CreatedOASISType = new API.Core.Helpers.EnumValue<API.Core.Enums.OASISType>(API.Core.Enums.OASISType.STARCLI) };
        //public List<IPlanet> Planets { get; set; } = new List<IPlanet>();

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

        //public SolarSystem(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.SolarSystem) { }
        public SolarSystem(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, HolonType.SolarSystem, autoLoad) { }

        private void RegisterAllCelestialBodies()
        {
            base.UnregisterAllCelestialBodies();
            base.RegisterCelestialBodies(this.Planets, false);
            base.RegisterCelestialBodies(this.Asteroids, false);
            base.RegisterCelestialBodies(this.Comets, false);
            base.RegisterCelestialBodies(this.Meteroids, false);
        }

        //protected override void RegisterCelestialBodies(IEnumerable<ICelestialBody> celestialBodies)
        ////protected override void RegisterCelestialBodies()
        //{
        //    List<ICelestialBody> c
        //    celestialBodies = Planets;
        //    celestialBodies.Add

        //    base.RegisterCelestialBodies(celestialBodies);
        //}
    }
}