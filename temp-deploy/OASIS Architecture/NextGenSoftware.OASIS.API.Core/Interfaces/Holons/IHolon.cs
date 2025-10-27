using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IHolon : ISemanticHolon
    {
        //TODO: TEMP MOVED TO HOLONBASE TILL REFACTOR CODEBASE.
        //EnumValue<OASISType> CreatedOASISType { get; set; }
        //EnumValue<ProviderType> CreatedProviderType { get; set; }
        //GlobalHolonData GlobalHolonData { get; set; }
        //EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }
        //bool IsChanged { get; set; }
        //bool IsNewHolon { get; set; }
        //bool IsSaving { get; set; }
        //IHolon Original { get; set; }
        //Guid PreviousVersionId { get; set; }
        //Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }
        //Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        //Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }

        GlobalHolonData GlobalHolonData { get; set; }


        //FROM iCELESTIALHOLON - TODO: NEED TO REFFACTOR CODEBASE LATER TO USE ICELESTIALHOLON INSTEAD OF IHolon WHERE APPROPRIATE.
        IList<INode> Nodes { get; set; }
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

        event EventDelegates.HolonsLoaded OnChildrenLoaded;
        event EventDelegates.HolonsError OnChildrenLoadError;
        event EventDelegates.HolonDeleted OnDeleted;
        event EventDelegates.HolonError OnError;
        event EventDelegates.HolonAdded OnHolonAdded;
        event EventDelegates.HolonRemoved OnHolonRemoved;
        event EventDelegates.Initialized OnInitialized;
        event EventDelegates.HolonLoaded OnLoaded;
        event EventDelegates.HolonSaved OnSaved;
        event PropertyChangedEventHandler PropertyChanged;

        OASISResult<IHolon> AddHolon(IHolon holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T> AddHolon<T>(T holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        Task<OASISResult<IHolon>> AddHolonAsync(IHolon holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> AddHolonAsync<T>(T holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        //OASISResult<IHolon> Delete(bool softDelete = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<IHolon>> DeleteAsync(bool softDelete = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IHolon> Delete(Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IHolon>> DeleteAsync(Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        bool HasHolonChanged(bool checkChildren = true);
        OASISResult<IHolon> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        Task<OASISResult<IHolon>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        OASISResult<IEnumerable<IHolon>> LoadChildHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true);
        OASISResult<IEnumerable<T>> LoadChildHolons<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true) where T : IHolon, new();
        Task<OASISResult<IEnumerable<IHolon>>> LoadChildHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true);
        Task<OASISResult<IEnumerable<T>>> LoadChildHolonsAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true) where T : IHolon, new();
        void NotifyPropertyChanged(string propertyName);
        //OASISResult<IHolon> RemoveHolon(IHolon holon, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<IHolon>> RemoveHolonAsync(IHolon holon, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IHolon> RemoveHolon(IHolon holon, Guid avatarId, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IHolon>> RemoveHolonAsync(IHolon holon, Guid avatarId, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default);

        OASISResult<IHolon> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        Task<OASISResult<IHolon>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
    }
}