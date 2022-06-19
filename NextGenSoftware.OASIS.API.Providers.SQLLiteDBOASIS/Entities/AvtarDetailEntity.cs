using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

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
        public List<HeartRateEntryModel> HeartRateData { get; set; }
        public List<AvatarGiftModel> Gifts { get; set; } = new();
        public AvatarChakraModel Chakras { get; set; } = new();
        public AvatarAuraModel Aura { get; set; } = new();
        public AvatarStatsModel Stats { get; set; } = new();
        public List<GeneKeyModel> GeneKeys { get; set; } = new();
        public AvatarHumanDesignModel HumanDesign { get; set; } = new();
        public AvatarSkillsModel Skills { get; set; } = new(); 
        public AvatarAttributesModel Attributes { get; set; } = new();
        public AvatarSuperPowersModel SuperPowers { get; set; } = new();
        public List<SpellModel> Spells { get; set; } = new();
        public List<AchievementModel> Achievements { get; set; } = new();
        public List<InventoryItemModel> Inventory { get; set; } = new();
        public List<MetaDataModel> MetaData { get; set; }
        public int Level { get; set; }
        public List<KarmaAkashicRecordModel> KarmaAkashicRecords { get; set; }
        public Guid ParentOmniverseId { get; set; }
        public Guid ParentMultiverseId { get; set; }
        public Guid ParentUniverseId { get; set; }
        public UniverseModel ParentUniverse { get; set; }
        public Guid ParentDimensionId { get; set; }
        public DimensionModel ParentDimension { get; set; }
        public DimensionLevel DimensionLevel { get; set; }
        public SubDimensionLevel SubDimensionLevel { get; set; }
        public Guid ParentGalaxyClusterId { get; set; }
        public GalaxyClusterModel ParentGalaxyCluster { get; set; }
        public Guid ParentGalaxyId { get; set; }
        public GalaxyModel ParentGalaxy { get; set; }
        public Guid ParentSolarSystemId { get; set; }
        public SolarSystemModel ParentSolarSystem { get; set; }
        public Guid ParentGreatGrandSuperStarId { get; set; }
        public GreatGrandSuperStarModel ParentGreatGrandSuperStar { get; set; }
        public Guid ParentGrandSuperStarId { get; set; }
        public GrandSuperStarModel ParentGrandSuperStar { get; set; }
        public Guid ParentSuperStarId { get; set; }
        public SuperStarModel ParentSuperStar { get; set; }
        public Guid ParentStarId { get; set; }
        public StarModel ParentStar { get; set; }
        public Guid ParentPlanetId { get; set; }
        public PlanetModel ParentPlanet { get; set; }
        public Guid ParentMoonId { get; set; }
        public MoonModel ParentMoon { get; set; }
        public Guid ParentCelestialSpaceId { get; set; }
        public CelestialSpaceAbstract ParentCelestialSpace { get; set; }
        public Guid ParentCelestialBodyId { get; set; }
        public CelestialBodyAbstract ParentCelestialBody { get; set; }
        public Guid ParentZomeId { get; set; }
        public Guid ParentHolonId { get; set; }
        public HolonEntity ParentHolon { get; set; }
        public List<HolonEntity> Children { get; set; }
        public string Description { get; set; }
        public int Version { get; set; }
        public Guid VersionId { get; set; }
        public Guid PreviousVersionId { get; set; }
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