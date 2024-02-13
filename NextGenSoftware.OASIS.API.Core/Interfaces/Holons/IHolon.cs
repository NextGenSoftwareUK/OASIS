using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IHolon : IHolonBase
    {
        //Guid Id { get; set; }
        //string Name { get; set; }
        //string Description { get; set; }
        //public IHolon Original { get; set; }
        //public bool IsNewHolon { get; set; }
        //public bool IsChanged { get; set; }
        //Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        //Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } 
        //Dictionary<string, string> MetaData { get; set; } 
        //HolonType HolonType { get; set; }
        public Guid ParentOmniverseId { get; set; } //The Omniverse this Holon belongs to.
        public IOmiverse ParentOmniverse { get; set; } //The Omniverse this Holon belongs to.
        public Guid ParentMultiverseId { get; set; } //The Multiverse this Holon belongs to.
        public IMultiverse ParentMultiverse { get; set; } //The Multiverse this Holon belongs to.
        public Guid ParentUniverseId { get; set; } //The Universe this Holon belongs to.
        public IUniverse ParentUniverse { get; set; } //The Universe this Holon belongs to.
        public Guid ParentDimensionId { get; set; } //The Dimension this Holon belongs to.
        public IDimension ParentDimension { get; set; } //The Dimension this Holon belongs to.
        public DimensionLevel DimensionLevel { get; set; } //The dimension this Holon belongs to (a holon can have a different version of itself in each dimension (asscended/evolved versions of itself).
        public SubDimensionLevel SubDimensionLevel { get; set; } //The sub-dimension/plane this Holon belongs to.
        public Guid ParentGalaxyClusterId { get; set; } //The GalaxyCluster this Holon belongs to.
        public IGalaxyCluster ParentGalaxyCluster { get; set; } //The GalaxyCluster this Holon belongs to.
        public Guid ParentGalaxyId { get; set; } //The Galaxy this Holon belongs to.
        public IGalaxy ParentGalaxy { get; set; } //The Galaxy this Holon belongs to.
        public Guid ParentSolarSystemId { get; set; } //The SolarSystem this Holon belongs to.
        public ISolarSystem ParentSolarSystem { get; set; } //The SolarSystem this Holon belongs to.
        public Guid ParentGreatGrandSuperStarId { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        public Guid ParentGrandSuperStarId { get; set; } //The GrandSuperStar this Holon belongs to.
        public IGrandSuperStar ParentGrandSuperStar { get; set; } //The GrandSuperStar this Holon belongs to.
        public Guid ParentSuperStarId { get; set; } //The SuperStar this Holon belongs to.
        public ISuperStar ParentSuperStar { get; set; } //The SuperStar this Holon belongs to.
        public Guid ParentStarId { get; set; } //The Star this Holon belongs to.
        public IStar ParentStar { get; set; } //The Star this Holon belongs to.
        public Guid ParentPlanetId { get; set; } //The Planet this Holon belongs to.
        public IPlanet ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public Guid ParentMoonId { get; set; } //The Moon this Holon belongs to.    
        public IMoon ParentMoon { get; set; } //The Moon this Holon belongs to.
        public Guid ParentCelestialSpaceId { get; set; } // The CelestialSpace Id this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
        public ICelestialSpace ParentCelestialSpace { get; set; } // The CelestialSpace this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
        public Guid ParentCelestialBodyId { get; set; } // The CelestialBody Id this holon belongs to (this could be a moon, planet, star, etc). 
        public ICelestialBody ParentCelestialBody { get; set; } // The CelestialBody  this holon belongs to (this could be a moon, planet, star, etc). 
        public Guid ParentZomeId { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public Guid ParentHolonId { get; set; }
        public IHolon ParentHolon { get; set; }
        IEnumerable<IHolon> Children { get; set; }
        ObservableCollection<IHolon> ChildrenTest { get; set; }
        //Guid CreatedByAvatarId { get; set; }
        //Avatar CreatedByAvatar { get; set; }
        //DateTime CreatedDate { get; set; }
        //Guid ModifiedByAvatarId { get; set; }
        //Avatar ModifiedByAvatar { get; set; }
        //DateTime ModifiedDate { get; set; }
        //Guid DeletedByAvatarId { get; set; }
        //Avatar DeletedByAvatar { get; set; }
        //DateTime DeletedDate { get; set; }
        //bool IsActive { get; set; }
        //int Version { get; set; }
        //EnumValue<ProviderType> CreatedProviderType { get; set; }
        ObservableCollection<INode> Nodes { get; set; }

        //  bool HasHolonChanged(bool checkChildren = true);

        public OASISResult<IEnumerable<IHolon>> LoadChildHolons(HolonType holonType = HolonType.All, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, bool cache = true);
    }
}