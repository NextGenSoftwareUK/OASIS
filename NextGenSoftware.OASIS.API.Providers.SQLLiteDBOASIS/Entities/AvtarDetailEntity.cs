using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities
{
    [Table("AvatarDetails")]
    public class AvatarDetailEntity
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public ConsoleColor FavouriteColour { get; set; }
        public ConsoleColor STARCLIColour { get; set; }
        public DateTime DOB { get; set; }
        public string Address { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string Postcode { get; set; }
        public string Mobile { get; set; }
        public string Landline { get; set; }
        public string UmaJson { get; set; }
        public string Portrait { get; set; }
        public string Model3D { get; set; }
        public int Karma { get; set; }
        public int XP { get; set; }
        public List<HeartRateEntry> HeartRateData { get; set; }
        public IOmiverse Omniverse { get; set; }
        public List<AvatarGift> Gifts { get; set; } = new();
        public Dictionary<DimensionLevel, Guid> DimensionLevelIds { get; set; }
        public Dictionary<DimensionLevel, IHolon> DimensionLevels { get; set; }
        public AvatarChakras Chakras { get; set; } = new();
        public AvatarAura Aura { get; set; } = new();
        public AvatarStats Stats { get; set; } = new();
        public List<GeneKey> GeneKeys { get; set; } = new();
        public HumanDesign HumanDesign { get; set; } = new();
        public AvatarSkills Skills { get; set; } = new(); 
        public AvatarAttributes Attributes { get; set; } = new();
        public AvatarSuperPowers SuperPowers { get; set; } = new();
        public List<Spell> Spells { get; set; } = new();
        public List<Achievement> Achievements { get; set; } = new();
        public List<InventoryItem> Inventory { get; set; } = new();
        public int Level { get; set; }
        public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        public Guid ParentOmniverseId { get; set; }
        public IOmiverse ParentOmniverse { get; set; }
        public Guid ParentMultiverseId { get; set; }
        public IMultiverse ParentMultiverse { get; set; }
        public Guid ParentUniverseId { get; set; }
        public IUniverse ParentUniverse { get; set; }
        public Guid ParentDimensionId { get; set; }
        public IDimension ParentDimension { get; set; }
        public DimensionLevel DimensionLevel { get; set; }
        public SubDimensionLevel SubDimensionLevel { get; set; }
        public Guid ParentGalaxyClusterId { get; set; }
        public IGalaxyCluster ParentGalaxyCluster { get; set; }
        public Guid ParentGalaxyId { get; set; }
        public IGalaxy ParentGalaxy { get; set; }
        public Guid ParentSolarSystemId { get; set; }
        public ISolarSystem ParentSolarSystem { get; set; }
        public Guid ParentGreatGrandSuperStarId { get; set; }
        public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; }
        public Guid ParentGrandSuperStarId { get; set; }
        public IGrandSuperStar ParentGrandSuperStar { get; set; }
        public Guid ParentSuperStarId { get; set; }
        public ISuperStar ParentSuperStar { get; set; }
        public Guid ParentStarId { get; set; }
        public IStar ParentStar { get; set; }
        public Guid ParentPlanetId { get; set; }
        public IPlanet ParentPlanet { get; set; }
        public Guid ParentMoonId { get; set; }
        public IMoon ParentMoon { get; set; }
        public Guid ParentCelestialSpaceId { get; set; }
        public ICelestialSpace ParentCelestialSpace { get; set; }
        public Guid ParentCelestialBodyId { get; set; }
        public ICelestialBody ParentCelestialBody { get; set; }
        public Guid ParentZomeId { get; set; }
        public IZome ParentZome { get; set; }
        public Guid ParentHolonId { get; set; }
        public HolonEntity ParentHolon { get; set; }
        public IEnumerable<IHolon> Children { get; set; }
        public ObservableCollection<IHolon> ChildrenTest { get; set; }
        public ObservableCollection<INode> Nodes { get; set; }
        public string Description { get; set; }
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        public Dictionary<string, string> MetaData { get; set; }
        public int Version { get; set; }
        public Guid VersionId { get; set; }
        public Guid PreviousVersionId { get; set; }
        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }
        public bool IsActive { get; set; }
        public bool IsChanged { get; set; }
        public bool IsNewHolon { get; set; }
        public bool IsSaving { get; set; }
        public HolonEntity Original { get; set; }
        public AvatarEntity CreatedByAvatar { get; set; }
        public Guid CreatedByAvatarId { get; set; }
        public DateTime CreatedDate { get; set; }
        public AvatarEntity ModifiedByAvatar { get; set; }
        public Guid ModifiedByAvatarId { get; set; }
        public DateTime ModifiedDate { get; set; }
        public AvatarEntity DeletedByAvatar { get; set; }
        public Guid DeletedByAvatarId { get; set; }
        public DateTime DeletedDate { get; set; }
        public ProviderType CreatedProviderType { get; set; }
        public OASISType CreatedOASISType { get; set; }
    }
}