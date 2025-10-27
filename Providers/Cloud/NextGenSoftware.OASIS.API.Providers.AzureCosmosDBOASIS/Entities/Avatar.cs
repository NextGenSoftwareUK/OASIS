//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.API.Core.Objects;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Threading.Tasks;

//namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Entities
//{
//    public class Avatar : Entity, IAvatar
//    {
//        public Guid AvatarId
//        {
//            get { return base.Id; }
//            set { base.Id = value; }
//        }

//        public Dictionary<ProviderType, string> ProviderPrivateKey { get; set; } = new Dictionary<ProviderType, string>();
//        public Dictionary<ProviderType, string> ProviderPublicKey { get; set; } = new Dictionary<ProviderType, string>();
//        public Dictionary<ProviderType, string> ProviderUsername { get; set; } = new Dictionary<ProviderType, string>();
//        public Dictionary<ProviderType, string> ProviderWalletAddress { get; set; } = new Dictionary<ProviderType, string>();
//        public string Username { get; set; }
//        public string Password { get; set; }
//        public string Email { get; set; }
//        public string Title { get; set; }
//        public string FirstName { get; set; }
//        public string LastName { get; set; }
//        public ConsoleColor FavouriteColour { get; set; }
//        public ConsoleColor STARCLIColour { get; set; }

//        public string FullName
//        {
//            get
//            {
//                return string.Concat(Title, " ", FirstName, " ", LastName);
//            }
//        }

//        public DateTime DOB { get; set; }
//        public string Address { get; set; }
//        public int Karma { get; set; }

//        public int Level { get; set; }

//        public int XP { get; set; }
//        public List<AvatarGift> Gifts { get; set; }
//        public AvatarChakras Chakras { get; set; }
//        public AvatarAura Aura { get; set; }
//        public AvatarStats Stats { get; set; }
//        public List<GeneKey> GeneKeys { get; set; }
//        public HumanDesign HumanDesign { get; set; }
//        public AvatarSkills Skills { get; set; }
//        public AvatarAttributes Attributes { get; set; }
//        public AvatarSuperPowers SuperPowers { get; set; }
//        public List<Spell> Spells { get; set; }
//        public List<Achievement> Achievements { get; set; }
//        public List<InventoryItem> Inventory { get; set; }
//        public string Town { get; set; }
//        public string County { get; set; }
//        public string Country { get; set; }
//        public string Postcode { get; set; }
//        public string Mobile { get; set; }
//        public string Landline { get; set; }
//        public EnumValue<AvatarType> AvatarType { get; set; }
//        public EnumValue<OASISType> CreatedOASISType { get; set; }
//        public bool AcceptTerms { get; set; }
//        public string VerificationToken { get; set; }
//        public DateTime? Verified { get; set; }
//        public string ResetToken { get; set; }
//        public string JwtToken { get; set; }
//        public string RefreshToken { get; set; }
//        public DateTime? ResetTokenExpires { get; set; }
//        public DateTime? PasswordReset { get; set; }
//        public List<RefreshToken> RefreshTokens { get; set; }
//        public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
//        public string Name { get; set; }
//        public string Description { get; set; }
//        public IHolon Original { get; set; }
//        public bool IsNewHolon { get; set; }
//        public bool IsChanged { get; set; }
//        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
//        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
//        public Dictionary<string, string> MetaData { get; set; }
//        public HolonType HolonType { get; set; }
//        public Guid ParentOmniverseId { get; set; }
//        public IOmiverse ParentOmniverse { get; set; }
//        public Guid ParentMultiverseId { get; set; }
//        public IMultiverse ParentMultiverse { get; set; }
//        public Guid ParentUniverseId { get; set; }
//        public IUniverse ParentUniverse { get; set; }
//        public Guid ParentDimensionId { get; set; }
//        public IDimension ParentDimension { get; set; }
//        public Guid ParentGalaxyClusterId { get; set; }
//        public IGalaxyCluster ParentGalaxyCluster { get; set; }
//        public Guid ParentGalaxyId { get; set; }
//        public IGalaxy ParentGalaxy { get; set; }
//        public Guid ParentSolarSystemId { get; set; }
//        public ISolarSystem ParentSolarSystem { get; set; }
//        public Guid ParentGreatGrandSuperStarId { get; set; }
//        public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; }
//        public Guid ParentGrandSuperStarId { get; set; }
//        public IGrandSuperStar ParentGrandSuperStar { get; set; }
//        public Guid ParentSuperStarId { get; set; }
//        public ISuperStar ParentSuperStar { get; set; }
//        public Guid ParentStarId { get; set; }
//        public IStar ParentStar { get; set; }
//        public Guid ParentPlanetId { get; set; }
//        public IPlanet ParentPlanet { get; set; }
//        public Guid ParentMoonId { get; set; }
//        public IMoon ParentMoon { get; set; }
//        public Guid ParentZomeId { get; set; }
//        public IZome ParentZome { get; set; }
//        public Guid ParentHolonId { get; set; }
//        public IHolon ParentHolon { get; set; }
//        public IEnumerable<IHolon> Children { get; set; }
//        public ObservableCollection<IHolon> ChildrenTest { get; set; }
//        public Guid CreatedByAvatarId { get; set; }
//        public Core.Holons.Avatar CreatedByAvatar { get; set; }
//        public DateTime CreatedDate { get; set; }
//        public Guid ModifiedByAvatarId { get; set; }
//        public Core.Holons.Avatar ModifiedByAvatar { get; set; }
//        public DateTime ModifiedDate { get; set; }
//        public Guid DeletedByAvatarId { get; set; }
//        public Core.Holons.Avatar DeletedByAvatar { get; set; }
//        public DateTime DeletedDate { get; set; }
//        public bool IsActive { get; set; }
//        public int Version { get; set; }
//        public EnumValue<ProviderType> CreatedProviderType { get; set; }
//        public ObservableCollection<INode> Nodes { get; set; }

//        public bool IsVerified { get; }

//        public DateTime? LastBeamedIn { get; set; }
//        public DateTime? LastBeamedOut { get; set; }
//        public bool IsBeamedIn { get; set; }
//        public string Image2D { get; set; }
//        public Guid PreviousVersionId { get; set; }
//        public Dictionary<ProviderType, string> PreviousVersionProviderKey { get; set; }
//        public Guid VersionId { get; set; }
//        //public Dictionary<ProviderType, List<IProviderWallet>> ProviderWallets { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//        public Dictionary<ProviderType, List<IProviderWallet>> ProviderWallets { get; set; } = new Dictionary<ProviderType, List<IProviderWallet>>();
//        //public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>();
//        //public bool IsSaving { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//        public bool IsSaving { get; set; } = false;

//        public event PropertyChangedEventHandler PropertyChanged;

//        public bool HasHolonChanged(bool checkChildren = true)
//        {
//            throw new NotImplementedException();
//        }

//        public KarmaAkashicRecord KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<KarmaAkashicRecord> KarmaEarntAsync(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public KarmaAkashicRecord KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<KarmaAkashicRecord> KarmaLostAsync(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public void NotifyPropertyChanged(string propertyName)
//        {
//            throw new NotImplementedException();
//        }

//        public bool OwnsToken(string token)
//        {
//            throw new NotImplementedException();
//        }

//        public IAvatar Save()
//        {
//            throw new NotImplementedException();
//        }

//        public OASISResult<IAvatar> Save(AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IAvatar> SaveAsync()
//        {
//            throw new NotImplementedException();
//        }

//        public Task<OASISResult<IAvatar>> SaveAsync(AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
