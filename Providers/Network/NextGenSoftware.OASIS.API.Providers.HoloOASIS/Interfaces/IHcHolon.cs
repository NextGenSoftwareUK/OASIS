using System;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public interface IHcHolon : IHoloNETAuditEntryBase
    {
        #region IHolonBase Properties

        IList<IHolon> Children { get; set; } //Allows any holon to add any number of custom child holons to it.
        //IReadOnlyCollection<IHolon> AllChildren { get; set; } //Readonly collection of all the total children including all the zomes, celestialbodies, celestialspaces, moons, holons, planets, stars etc belong to the holon.
        //Guid CreatedByAvatarId { get; set; }
        //DateTime CreatedDate { get; set; }
        EnumValue<OASISType> CreatedOASISType { get; set; }
        EnumValue<ProviderType> CreatedProviderType { get; set; }
        string CustomKey { get; set; }
        //Guid DeletedByAvatarId { get; set; }
        //DateTime DeletedDate { get; set; }
        string Description { get; set; }
        HolonType HolonType { get; set; }
        EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }
        bool IsActive { get; set; }
        //bool IsChanged { get; set; }
        //bool IsNewHolon { get; set; }
        //bool IsSaving { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        //Guid ModifiedByAvatarId { get; set; }
        //DateTime ModifiedDate { get; set; }
        string Name { get; set; }
        //IHolon Original { get; set; }
        //IHolon ParentHolon { get; set; }
        Guid ParentHolonId { get; set; }
        Guid PreviousVersionId { get; set; }
        Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }
        Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        int Version { get; set; }
        Guid VersionId { get; set; }
        string ChildIdListCache { get; set; } //This will store the list of id's for the direct childen of this holon.
        string AllChildIdListCache { get; set; } //This will store the list of id's for the ALL the childen of this holon (including all sub-childen).

        #endregion

        #region IHolon Properties

        Guid ParentOmniverseId { get; set; } //The Omniverse this Holon belongs to.
        //IOmiverse ParentOmniverse { get; set; } //The Omniverse this Holon belongs to.
        Guid ParentMultiverseId { get; set; } //The Multiverse this Holon belongs to.
        //IMultiverse ParentMultiverse { get; set; } //The Multiverse this Holon belongs to.
        Guid ParentUniverseId { get; set; } //The Universe this Holon belongs to.
        //IUniverse ParentUniverse { get; set; } //The Universe this Holon belongs to.
        Guid ParentDimensionId { get; set; } //The Dimension this Holon belongs to.
        //IDimension ParentDimension { get; set; } //The Dimension this Holon belongs to.
        DimensionLevel DimensionLevel { get; set; } //The dimension this Holon belongs to (a holon can have a different version of itself in each dimension (asscended/evolved versions of itself).
        SubDimensionLevel SubDimensionLevel { get; set; } //The sub-dimension/plane this Holon belongs to.
        Guid ParentGalaxyClusterId { get; set; } //The GalaxyCluster this Holon belongs to.
        //IGalaxyCluster ParentGalaxyCluster { get; set; } //The GalaxyCluster this Holon belongs to.
        Guid ParentGalaxyId { get; set; } //The Galaxy this Holon belongs to.
        //IGalaxy ParentGalaxy { get; set; } //The Galaxy this Holon belongs to.
        Guid ParentSolarSystemId { get; set; } //The SolarSystem this Holon belongs to.
        //ISolarSystem ParentSolarSystem { get; set; } //The SolarSystem this Holon belongs to.
        Guid ParentGreatGrandSuperStarId { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        //IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        Guid ParentGrandSuperStarId { get; set; } //The GrandSuperStar this Holon belongs to.
        //IGrandSuperStar ParentGrandSuperStar { get; set; } //The GrandSuperStar this Holon belongs to.
        Guid ParentSuperStarId { get; set; } //The SuperStar this Holon belongs to.
        //ISuperStar ParentSuperStar { get; set; } //The SuperStar this Holon belongs to.
        Guid ParentStarId { get; set; } //The Star this Holon belongs to.
       // IStar ParentStar { get; set; } //The Star this Holon belongs to.
        Guid ParentPlanetId { get; set; } //The Planet this Holon belongs to.
        //IPlanet ParentPlanet { get; set; } //The Planet this Holon belongs to.
        Guid ParentMoonId { get; set; } //The Moon this Holon belongs to.    
        //IMoon ParentMoon { get; set; } //The Moon this Holon belongs to.
        Guid ParentCelestialSpaceId { get; set; } // The CelestialSpace Id this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
        //ICelestialSpace ParentCelestialSpace { get; set; } // The CelestialSpace this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
        Guid ParentCelestialBodyId { get; set; } // The CelestialBody Id this holon belongs to (this could be a moon, planet, star, etc). 
        //ICelestialBody ParentCelestialBody { get; set; } // The CelestialBody  this holon belongs to (this could be a moon, planet, star, etc). 
        Guid ParentZomeId { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        //IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        //ObservableCollection<INode> Nodes { get; set; }
        IList<INode> Nodes { get; set; }

        #endregion
    }
}