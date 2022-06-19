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
    [Table("avatar_details")]
    public class AvatarDetailEntity
    {
        [Column("id", TypeName = "NVARCHAR(64)")]
        public Guid Id { get; set; }

        [Column("username", TypeName = "NVARCHAR(100)")]
        public string Username { get; set; }

        [Column("email", TypeName = "NVARCHAR(100)")]
        public string Email { get; set; }

        [Column("favourite_colour", TypeName = "INTEGER")]
        public ConsoleColor FavouriteColour { get; set; }

        [Column("star_cli_colour", TypeName = "INTEGER")]
        public ConsoleColor STARCLIColour { get; set; }

        [Column("dob", TypeName = "DATETIME")] public DateTime DOB { get; set; }

        [Column("address", TypeName = "NVARCHAR(100)")]
        public string Address { get; set; }

        [Column("town", TypeName = "NVARCHAR(100)")]
        public string Town { get; set; }

        [Column("county", TypeName = "NVARCHAR(100)")]
        public string County { get; set; }

        [Column("country", TypeName = "NVARCHAR(100)")]
        public string Country { get; set; }

        [Column("postcode", TypeName = "NVARCHAR(100)")]
        public string Postcode { get; set; }

        [Column("mobile", TypeName = "NVARCHAR(100)")]
        public string Mobile { get; set; }

        [Column("landline", TypeName = "NVARCHAR(100)")]
        public string Landline { get; set; }

        [Column("uma_json", TypeName = "NVARCHAR(100)")]
        public string UmaJson { get; set; }

        [Column("portrait", TypeName = "NVARCHAR(100)")]
        public string Portrait { get; set; }

        [Column("model_3d", TypeName = "NVARCHAR(100)")]
        public string Model3D { get; set; }

        [Column("karma", TypeName = "INTEGER")]
        public int Karma { get; set; }

        [Column("xp", TypeName = "INTEGER")] public int XP { get; set; }
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

        [Column("level", TypeName = "INTEGER")]
        public int Level { get; set; }

        public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }

        [Column("parent_omniverse_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentOmniverseId { get; set; }

        public IOmiverse ParentOmniverse { get; set; }

        [Column("parent_multiverse_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentMultiverseId { get; set; }

        public IMultiverse ParentMultiverse { get; set; }

        [Column("parent_universe_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentUniverseId { get; set; }

        public IUniverse ParentUniverse { get; set; }

        [Column("parent_dimension_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentDimensionId { get; set; }

        public IDimension ParentDimension { get; set; }

        [Column("dimension_level", TypeName = "INTEGER")]
        public DimensionLevel DimensionLevel { get; set; }

        [Column("sub_dimension_level", TypeName = "INTEGER")]
        public SubDimensionLevel SubDimensionLevel { get; set; }

        [Column("parent_galaxy_cluster_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentGalaxyClusterId { get; set; }

        public IGalaxyCluster ParentGalaxyCluster { get; set; }

        [Column("parent_galaxy_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentGalaxyId { get; set; }

        public IGalaxy ParentGalaxy { get; set; }

        [Column("parent_solar_system_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentSolarSystemId { get; set; }

        public ISolarSystem ParentSolarSystem { get; set; }

        [Column("parent_great_grand_super_star_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentGreatGrandSuperStarId { get; set; }

        public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; }

        [Column("parent_grand_super_star_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentGrandSuperStarId { get; set; }

        public IGrandSuperStar ParentGrandSuperStar { get; set; }

        [Column("parent_super_star_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentSuperStarId { get; set; }

        public ISuperStar ParentSuperStar { get; set; }

        [Column("parent_star_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentStarId { get; set; }

        public IStar ParentStar { get; set; }

        [Column("parent_planet_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentPlanetId { get; set; }

        public IPlanet ParentPlanet { get; set; }

        [Column("parent_moon_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentMoonId { get; set; }

        public IMoon ParentMoon { get; set; }

        [Column("parent_celestial_space_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentCelestialSpaceId { get; set; }

        public ICelestialSpace ParentCelestialSpace { get; set; }

        [Column("parent_celestial_body_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentCelestialBodyId { get; set; }

        public ICelestialBody ParentCelestialBody { get; set; }

        [Column("parent_zome_Id", TypeName = "NVARCHAR(64)")]
        public Guid ParentZomeId { get; set; }

        public IZome ParentZome { get; set; }

        [Column("parent_holon_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentHolonId { get; set; }

        public HolonEntity ParentHolon { get; set; }
        public IEnumerable<IHolon> Children { get; set; }
        public ObservableCollection<IHolon> ChildrenTest { get; set; }
        public ObservableCollection<INode> Nodes { get; set; }

        [Column("description", TypeName = "NVARCHAR(300)")]
        public string Description { get; set; }

        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        public Dictionary<string, string> MetaData { get; set; }

        [Column("version", TypeName = "INTEGER")]
        public int Version { get; set; }

        [Column("version_id", TypeName = "NVARCHAR(64)")]
        public Guid VersionId { get; set; }

        [Column("previous_version_id", TypeName = "NVARCHAR(64)")]
        public Guid PreviousVersionId { get; set; }

        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }

        [Column("is_active", TypeName = "BOOLEAN")]
        public bool IsActive { get; set; }

        [Column("is_changed", TypeName = "BOOLEAN")]
        public bool IsChanged { get; set; }

        [Column("is_new_holon", TypeName = "BOOLEAN")]
        public bool IsNewHolon { get; set; }

        [Column("is_saving", TypeName = "BOOLEAN")]
        public bool IsSaving { get; set; }

        public HolonEntity Original { get; set; }
        public AvatarEntity CreatedByAvatar { get; set; }

        [Column("created_by_avatar_id", TypeName = "NVARCHAR(64)")]
        public Guid CreatedByAvatarId { get; set; }

        [Column("created_date", TypeName = "DATETIME")]
        public DateTime CreatedDate { get; set; }

        public AvatarEntity ModifiedByAvatar { get; set; }

        [Column("modified_by_avatar_id", TypeName = "NVARCHAR(64)")]
        public Guid ModifiedByAvatarId { get; set; }

        [Column("modified_date", TypeName = "DATETIME")]
        public DateTime ModifiedDate { get; set; }

        public AvatarEntity DeletedByAvatar { get; set; }

        [Column("deleted_by_avatar_id", TypeName = "NVARCHAR(64)")]
        public Guid DeletedByAvatarId { get; set; }

        [Column("deleted_date", TypeName = "DATETIME")]
        public DateTime DeletedDate { get; set; }

        [Column("created_provider_type", TypeName = "INTEGER")]
        public ProviderType CreatedProviderType { get; set; }

        [Column("created_oasis_type", TypeName = "INTEGER")]
        public OASISType CreatedOASISType { get; set; }
    }
}