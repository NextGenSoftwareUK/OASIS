//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.API.Core.Objects;

//namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Entities
//{
//    public class AvatarDetail : Entity, IAvatarDetail
//    {        
//        public string Username { get ; set ; }
//        public string Email { get ; set ; }
//        public List<Achievement> Achievements { get ; set ; }
//        public string Address { get ; set ; }
//        public AvatarAttributes Attributes { get ; set ; }
//        public AvatarAura Aura { get ; set ; }
//        public AvatarChakras Chakras { get ; set ; }
//        public string Country { get ; set ; }
//        public string County { get ; set ; }
//        public Dictionary<DimensionLevel, Guid> DimensionLevelIds { get; set ; }
//        public Dictionary<DimensionLevel, IHolon> DimensionLevels { get ; set ; }
//        public DateTime DOB { get ; set ; }
//        public ConsoleColor FavouriteColour { get; set ; }
//        public List<GeneKey> GeneKeys { get ; set ; }
//        public List<AvatarGift> Gifts { get ; set ; }
//        public List<HeartRateEntry> HeartRateData { get ; set ; }
//        public HumanDesign HumanDesign { get ; set ; }
//        public string Portrait { get ; set ; }
//        public List<InventoryItem> Inventory { get ; set ; }
//        public int Karma { get ; set ; }
//        public List<KarmaAkashicRecord> KarmaAkashicRecords { get ; set ; }
//        public string Landline { get ; set ; }
//        public int Level { get; set; }
//        public string Mobile { get ; set ; }
//        public string Model3D { get ; set ; }
//        public IOmiverse Omniverse { get ; set ; }
//        public string Postcode { get ; set ; }
//        public AvatarSkills Skills { get ; set ; }
//        public List<Spell> Spells { get; set ; }
//        public ConsoleColor STARCLIColour { get ; set ; }
//        public AvatarStats Stats { get ; set ; }
//        public AvatarSuperPowers SuperPowers { get ; set ; }
//        public string Town { get ; set ; }
//        public string UmaJson { get ; set ; }
//        public int XP { get ; set ; }
//        public Guid ParentOmniverseId { get ; set ; }
//        public IOmiverse ParentOmniverse { get ; set ; }
//        public Guid ParentMultiverseId { get ; set ; }
//        public IMultiverse ParentMultiverse { get ; set ; }
//        public Guid ParentUniverseId { get ; set ; }
//        public IUniverse ParentUniverse { get ; set ; }
//        public Guid ParentDimensionId { get ; set ; }
//        public IDimension ParentDimension { get ; set ; }
//        public DimensionLevel DimensionLevel { get ; set ; }
//        public SubDimensionLevel SubDimensionLevel { get ; set ; }
//        public Guid ParentGalaxyClusterId { get ; set ; }
//        public IGalaxyCluster ParentGalaxyCluster { get ; set ; }
//        public Guid ParentGalaxyId { get ; set ; }
//        public IGalaxy ParentGalaxy { get ; set ; }
//        public Guid ParentSolarSystemId { get ; set ; }
//        public ISolarSystem ParentSolarSystem { get ; set ; }
//        public Guid ParentGreatGrandSuperStarId { get ; set ; }
//        public IGreatGrandSuperStar ParentGreatGrandSuperStar { get ; set ; }
//        public Guid ParentGrandSuperStarId { get ; set ; }
//        public IGrandSuperStar ParentGrandSuperStar { get ; set ; }
//        public Guid ParentSuperStarId { get ; set ; }
//        public ISuperStar ParentSuperStar { get ; set ; }
//        public Guid ParentStarId { get ; set ; }
//        public IStar ParentStar { get ; set ; }
//        public Guid ParentPlanetId { get ; set ; }
//        public IPlanet ParentPlanet { get ; set ; }
//        public Guid ParentMoonId { get ; set ; }
//        public IMoon ParentMoon { get ; set ; }
//        public Guid ParentCelestialSpaceId { get ; set ; }
//        public ICelestialSpace ParentCelestialSpace { get ; set ; }
//        public Guid ParentCelestialBodyId { get ; set ; }
//        public ICelestialBody ParentCelestialBody { get ; set ; }
//        public Guid ParentZomeId { get ; set ; }
//        public IZome ParentZome { get ; set ; }
//        public Guid ParentHolonId { get ; set ; }
//        public IHolon ParentHolon { get ; set ; }
//        public IEnumerable<IHolon> Children { get ; set ; }
//        public ObservableCollection<IHolon> ChildrenTest { get ; set ; }
//        public ObservableCollection<INode> Nodes { get ; set ; }
//        public string Name { get ; set ; }
//        public string Description { get ; set ; }
//        public HolonType HolonType { get ; set ; }
//        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get ; set ; }
//        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get ; set ; }
//        public Dictionary<string, string> MetaData { get ; set ; }
//        public int Version { get ; set ; }
//        public Guid VersionId { get ; set ; }
//        public Guid PreviousVersionId { get ; set ; }
//        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get ; set ; }
//        public bool IsActive { get ; set ; }
//        public bool IsChanged { get ; set ; }
//        public bool IsNewHolon { get ; set ; }
//        public bool IsSaving { get ; set ; }
//        public IHolon Original { get ; set ; }
//        public Core.Holons.Avatar CreatedByAvatar { get ; set ; }
//        public Guid CreatedByAvatarId { get ; set ; }
//        public DateTime CreatedDate { get ; set ; }
//        public Core.Holons.Avatar ModifiedByAvatar { get ; set ; }
//        public Guid ModifiedByAvatarId { get ; set ; }
//        public DateTime ModifiedDate { get ; set ; }
//        public Core.Holons.Avatar DeletedByAvatar { get ; set ; }
//        public Guid DeletedByAvatarId { get ; set ; }
//        public DateTime DeletedDate { get ; set ; }
//        public EnumValue<ProviderType> CreatedProviderType { get ; set ; }
//        public EnumValue<OASISType> CreatedOASISType { get ; set ; }

//        public event PropertyChangedEventHandler PropertyChanged
//        {
//            add
//            {
//                throw new NotImplementedException();
//            }

//            remove
//            {
//                throw new NotImplementedException();
//            }
//        }

//        public bool HasHolonChanged(bool checkChildren)
//        {
//            throw new NotImplementedException();
//        }

//        public OASISResult<KarmaAkashicRecord> KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink, bool autoSave, int karmaOverride)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<OASISResult<KarmaAkashicRecord>> KarmaEarntAsync(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink, bool autoSave, int karmaOverride)
//        {
//            throw new NotImplementedException();
//        }

//        public OASISResult<KarmaAkashicRecord> KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink, bool autoSave, int karmaOverride)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<OASISResult<KarmaAkashicRecord>> KarmaLostAsync(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink, bool autoSave, int karmaOverride)
//        {
//            throw new NotImplementedException();
//        }

//        public bool LoadChildHolons()
//        {
//            throw new NotImplementedException();
//        }

//        public void NotifyPropertyChanged(string propertyName)
//        {
//            throw new NotImplementedException();
//        }

//        public OASISResult<IAvatarDetail> Save()
//        {
//            throw new NotImplementedException();
//        }

//        public Task<OASISResult<IAvatarDetail>> SaveAsync()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
