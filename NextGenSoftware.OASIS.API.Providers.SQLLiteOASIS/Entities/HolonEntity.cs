//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities
{
    public class HolonEntity : HolonBase, IHolon, INotifyPropertyChanged  // Equvilant to the Holon object in OASIS.API.Core.
    {
        public Guid HolonId { get; set; }
        public Guid ParentOmniverseId { get; set; } //The Omniverse this Holon belongs to.
        //[NotMapped]
        //public IOmiverse ParentOmniverse { get; set; } //The Omniverse this Holon belongs to.
        public Guid ParentMultiverseId { get; set; } //The Multiverse this Holon belongs to.
        //[NotMapped]
        //public IMultiverse ParentMultiverse { get; set; } //The Multiverse this Holon belongs to.
        public Guid ParentUniverseId { get; set; } //The Universe this Holon belongs to.
        //[NotMapped]
        //public IUniverse ParentUniverse { get; set; } //The Universe this Holon belongs to.
        public Guid ParentDimensionId { get; set; } //The Dimension this Holon belongs to.
        //[NotMapped]
        //public IDimension ParentDimension { get; set; } //The Dimension this Holon belongs to.
        //[NotMapped]
        //public DimensionLevel DimensionLevel { get; set; } //The dimension this Holon belongs to (a holon can have a different version of itself in each dimension (asscended/evolved versions of itself).
        //[NotMapped] 
        //public SubDimensionLevel SubDimensionLevel { get; set; } //The sub-dimension/plane this Holon belongs to.
        public Guid ParentGalaxyClusterId { get; set; } //The GalaxyCluster this Holon belongs to.
        //[NotMapped] 
        //public IGalaxyCluster ParentGalaxyCluster { get; set; } //The GalaxyCluster this Holon belongs to.
        public Guid ParentGalaxyId { get; set; } //The Galaxy this Holon belongs to.
        //[NotMapped]
        //public IGalaxy ParentGalaxy { get; set; } //The Galaxy this Holon belongs to.
        public Guid ParentSolarSystemId { get; set; } //The SolarSystem this Holon belongs to.
        //[NotMapped] 
        //public ISolarSystem ParentSolarSystem { get; set; } //The SolarSystem this Holon belongs to.
        public Guid ParentGreatGrandSuperStarId { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        //[NotMapped] 
        //public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        public Guid ParentGrandSuperStarId { get; set; } //The GrandSuperStar this Holon belongs to.
        //[NotMapped] 
        //public IGrandSuperStar ParentGrandSuperStar { get; set; } //The GrandSuperStar this Holon belongs to.
        public Guid ParentSuperStarId { get; set; } //The SuperStar this Holon belongs to.
        //[NotMapped] 
        //public ISuperStar ParentSuperStar { get; set; } //The SuperStar this Holon belongs to.
        public Guid ParentStarId { get; set; } //The Star this Holon belongs to.
        //public ICelestialBody ParentStar { get; set; } //The Star this Holon belongs to.
        //[NotMapped] 
        //public IStar ParentStar { get; set; } //The Star this Holon belongs to.
        public Guid ParentPlanetId { get; set; } //The Planet this Holon belongs to.
        //public ICelestialBody ParentPlanet { get; set; } //The Planet this Holon belongs to.
        //[NotMapped] 
        //public IPlanet ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public Guid ParentMoonId { get; set; } //The Moon this Holon belongs to.
        //public ICelestialBody ParentMoon { get; set; } //The Moon this Holon belongs to.
        //[NotMapped] 
        //public IMoon ParentMoon { get; set; } //The Moon this Holon belongs to.
        public Guid ParentCelestialSpaceId { get; set; } // The CelestialSpace Id this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
        //[NotMapped] 
        //public ICelestialSpace ParentCelestialSpace { get; set; } // The CelestialSpace this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
        public Guid ParentCelestialBodyId { get; set; } // The CelestialBody Id this holon belongs to (this could be a moon, planet, star, etc). 
        //[NotMapped] 
        //public ICelestialBody ParentCelestialBody { get; set; } // The CelestialBody  this holon belongs to (this could be a moon, planet, star, etc). 

        public Guid ParentZomeId { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OAPP's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        //[NotMapped] 
        //public IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OAPP's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public Guid ParentHolonId { get; set; }
        public IOmiverse ParentOmniverse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMultiverse ParentMultiverse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IUniverse ParentUniverse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDimension ParentDimension { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DimensionLevel DimensionLevel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public SubDimensionLevel SubDimensionLevel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IGalaxyCluster ParentGalaxyCluster { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IGalaxy ParentGalaxy { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISolarSystem ParentSolarSystem { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IGreatGrandSuperStar ParentGreatGrandSuperStar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IGrandSuperStar ParentGrandSuperStar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISuperStar ParentSuperStar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IStar ParentStar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IPlanet ParentPlanet { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMoon ParentMoon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ICelestialSpace ParentCelestialSpace { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ICelestialBody ParentCelestialBody { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IZome ParentZome { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IHolon ParentHolon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IEnumerable<IHolon> Children { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ObservableCollection<IHolon> ChildrenTest { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ObservableCollection<INode> Nodes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        //public HolonType HolonType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Dictionary<string, string> MetaData { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid VersionId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsNewHolon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsSaving { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IHolon Original { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Avatar CreatedByAvatar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Avatar ModifiedByAvatar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Avatar DeletedByAvatar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public EnumValue<ProviderType> CreatedProviderType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public EnumValue<OASISType> CreatedOASISType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Guid IHolonBase.CreatedByAvatarId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Guid IHolonBase.ModifiedByAvatarId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Guid IHolonBase.DeletedByAvatarId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasHolonChanged(bool checkChildren = true)
        {
            throw new NotImplementedException();
        }

        public bool LoadChildHolons()
        {
            throw new NotImplementedException();
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            throw new NotImplementedException();
        }
        //[NotMapped] 
        //public IHolon ParentHolon { get; set; }
        //[NotMapped] 
        //public IEnumerable<IHolon> Children { get; set; }
        //public ProviderType CreatedProviderType { get; set; }
        //[NotMapped] 
        //public ObservableCollection<INode> Nodes { get; set; }
    }
}