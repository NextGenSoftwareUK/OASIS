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
        public ISuperStar SuperStar { get; set; }
        public List<ISolarSystem> SolarSystems { get; set; } = new List<ISolarSystem>();
        public List<INebula> Nebulas { get; set; } = new List<INebula>();
        public List<IStar> Stars { get; set; } = new List<IStar>();
        public List<IPlanet> Planets { get; set; } = new List<IPlanet>();
        public List<IAsteroid> Asteroids { get; set; } = new List<IAsteroid>();
        public List<IComet> Comets { get; set; } = new List<IComet>();
        public List<IMeteroid> Meteroids { get; set; } = new List<IMeteroid>();

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
    }
}