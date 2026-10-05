using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities
{
    [BsonDiscriminator("OAPPDNA")]
    [BsonIgnoreExtraElements()]
    public class HolonBase : IHolonBase// Equvilant to the HolonBase object in OASIS.API.Core.
    {
        [BsonId]  
        [BsonRepresentation(BsonType.ObjectId)]  
        public string Id { get; set; }

        //public string CustomKey { get; set; } //A custom key that can be used to load the holon by (other than Id or ProviderKey).

        public bool IsChanged { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>(); //Unique key used by each provider (e.g. hashaddress in hc, accountname for Telos, id in MongoDB etc).    

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } = new Dictionary<ProviderType, Dictionary<string, string>>(); // Key/Value pair meta data can be stored here, which is unique for that provider.

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        //[BsonElement("MetaData2")]

        //[BsonDiscriminator("OASISGeoSpatialNFT")]
        
        public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>(); // Key/Value pair meta data can be stored here that applies globally across ALL providers.

        public Guid HolonId { get; set; } //Unique id within the OASIS.
        public string Name { get; set; }
        public string Description { get; set; }
        //  public string ProviderUniqueStorageKey { get; set; } //Unique key used by each provider (e.g. hashaddress in hc, etc).
        public HolonType HolonType { get; set; }
      //  public ProviderType CreatedProviderType { get; set; }
        public EnumValue<ProviderType> CreatedProviderType { get; set; } // The primary provider that this holon was originally saved with (it can then be auto-replicated to other providers to give maximum redundancy/speed via auto-load balancing etc).
        public EnumValue<OASISType> CreatedOASISType { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ModifiedDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DeletedDate { get; set; }

        public int Version { get; set; }
        public Guid PreviousVersionId { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>();

        public bool IsActive { get; set; }

        //  [BsonRepresentation(BsonType.ObjectId)]
        public string CreatedByAvatarId { get; set; }

       // [BsonRepresentation(BsonType.ObjectId)]
        public string ModifiedByAvatarId { get; set; }

       // [BsonRepresentation(BsonType.ObjectId)]
        public string DeletedByAvatarId { get; set; }
        public IList<IHolon> Children { get; set; }

        public virtual IReadOnlyCollection<IHolon> AllChildren
        {
            get
            {
                return Children.AsReadOnly();
            }
        }

        public string ChildIdListCache { get; set; } //This will store the list of id's for the direct childen of this holon.
        public string AllChildIdListCache { get; set; } //This will store the list of id's for the ALL the childen of this holon (including all sub-childen).

        //public ObservableCollection<IHolon> ChildrenTest { get; set; }
        public IAvatar CreatedByAvatar { get; set; }
        public IAvatar DeletedByAvatar { get; set; }
        public EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }
        public bool IsNewHolon { get; set; }
        public bool IsSaving { get; set; }
        public IAvatar ModifiedByAvatar { get; set; }
        public IHolon Original { get; set; }
        public IHolon ParentHolon { get; set; }
        public Guid ParentHolonId { get; set; }
        public Guid VersionId { get; set; }
        public GlobalHolonData GlobalHolonData { get; set; }
        Guid IAuditBase.CreatedByAvatarId { get; set; }
        Guid IAuditBase.DeletedByAvatarId { get; set; }
        //Guid IAuditBase.Id { get; set; }
        Guid IAuditBase.ModifiedByAvatarId { get; set; }

        //For some unknown reason MongoDB insists on having these properties from the IOAPPDNA interface here so it can deserilaize the OAPPDNAJSON key on the MetaData property! 
        //TODO: Want to find a way to remove this and force MongoDB not to try and map the JSON into properties, want it to just treat it as a normal string! ;-)
        public Guid OAPPId { get; set; }
        public string OAPPName { get; set; }
       // public string Description { get; set; }
       // public Guid CreatedByAvatarId { get; set; }
        //public string CreatedByAvatarName { get; set; }
        public string CreatedByAvatarUsername { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid PublishedByAvatarId { get; set; }
        //public string PublishedByAvatarName { get; set; }
        public string PublishedByAvatarUsername { get; set; }
        public DateTime PublishedOn { get; set; }
        public string PublishedPath { get; set; }
        public bool PublishedOnSTARNET { get; set; }
      //  public bool IsActive { get; set; }
        public OAPPType OAPPType { get; set; }
        public GenesisType GenesisType { get; set; }
        //public ICelestialBody CelestialBody { get; set; } //optional
        public Guid CelestialBodyId { get; set; }
        public string CelestialBodyName { get; set; }
        public HolonType CelestialBodyType { get; set; }
        public IEnumerable<IZome> Zomes { get; set; }
        public string LaunchTarget { get; set; }
        //public string Version { get; set; }
        public string STARODKVersion { get; set; }
        public string OASISVersion { get; set; }
        public string COSMICVersion { get; set; }
        Guid IHolonBase.Id { get => CreatedByAvatar.Id; set => CreatedByAvatar.Id = value; }

        public event EventDelegates.HolonsLoaded OnChildrenLoaded;
        public event EventDelegates.HolonsError OnChildrenLoadError;
        public event EventDelegates.HolonDeleted OnDeleted;
        public event EventDelegates.HolonError OnError;
        public event EventDelegates.HolonAdded OnHolonAdded;
        public event EventDelegates.HolonLoaded OnHolonLoaded;
        public event EventDelegates.HolonRemoved OnHolonRemoved;
        public event EventDelegates.HolonSaved OnHolonSaved;
        public event EventDelegates.HolonsLoaded OnHolonsLoaded;
        public event EventDelegates.HolonsSaved OnHolonsSaved;
        public event EventDelegates.Initialized OnInitialized;
        public event EventDelegates.HolonLoaded OnLoaded;
        public event EventDelegates.HolonSaved OnSaved;
        public event PropertyChangedEventHandler PropertyChanged;

        public OASISResult<IHolon> AddHolon(IHolon holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public OASISResult<T> AddHolon<T>(T holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IHolon>> AddHolonAsync(IHolon holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<T>> AddHolonAsync<T>(T holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IHolon> Delete(Guid avtatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IHolon>> DeleteAsync(Guid avtatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public bool HasHolonChanged(bool checkChildren = true)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IHolon> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IHolon>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IHolon>> LoadChildHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<T>> LoadChildHolons<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true) where T : IHolon, new()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IEnumerable<IHolon>>> LoadChildHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IEnumerable<T>>> LoadChildHolonsAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true) where T : IHolon, new()
        {
            throw new NotImplementedException();
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IHolon> RemoveHolon(IHolon holon, Guid avtatarId, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IHolon>> RemoveHolonAsync(IHolon holon, Guid avtatarId, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IHolon> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IHolon>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            throw new NotImplementedException();
        }
    }
}