using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities
{
    [Table("holons")]
    public class HolonEntity
    {
        [Column("holon_id", TypeName = "NVARCHAR(64)")]
        public Guid HolonId { get; set; }

        [Column("parent_omniverse_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentOmniverseId { get; set; }

        [Column("parent_multiverse_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentMultiverseId { get; set; }

        [Column("parent_universe_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentUniverseId { get; set; }

        [Column("parent_dimension_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentDimensionId { get; set; }

        [Column("parent_galaxy_cluster_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentGalaxyClusterId { get; set; }

        [Column("parent_galaxy_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentGalaxyId { get; set; }

        [Column("parent_solar_system_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentSolarSystemId { get; set; }

        [Column("parent_great_grand_super_star_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentGreatGrandSuperStarId { get; set; }

        [Column("parent_grand_super_star_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentGrandSuperStarId { get; set; }

        [Column("parent_super_star_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentSuperStarId { get; set; }

        [Column("parent_star_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentStarId { get; set; }

        [Column("parent_planet_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentPlanetId { get; set; }

        [Column("parent_moon_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentMoonId { get; set; }

        [Column("parent_celestial_space_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentCelestialSpaceId { get; set; }

        [Column("parent_celestial_body_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentCelestialBodyId { get; set; }

        [Column("parent_zome_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentZomeId { get; set; }

        [Column("parent_holon_id", TypeName = "NVARCHAR(64)")]
        public Guid ParentHolonId { get; set; }

        public IOmiverse ParentOmniverse { get; set; }
        public IMultiverse ParentMultiverse { get; set; }
        public IUniverse ParentUniverse { get; set; }
        public IDimension ParentDimension { get; set; }

        [Column("dimension_level", TypeName = "INTEGER")]
        public DimensionLevel DimensionLevel { get; set; }

        [Column("sub_dimension_level", TypeName = "INTEGER")]
        public SubDimensionLevel SubDimensionLevel { get; set; }

        public IGalaxyCluster ParentGalaxyCluster { get; set; }
        public IGalaxy ParentGalaxy { get; set; }
        public ISolarSystem ParentSolarSystem { get; set; }
        public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; }
        public IGrandSuperStar ParentGrandSuperStar { get; set; }
        public ISuperStar ParentSuperStar { get; set; }
        public IStar ParentStar { get; set; }
        public IPlanet ParentPlanet { get; set; }
        public IMoon ParentMoon { get; set; }
        public ICelestialSpace ParentCelestialSpace { get; set; }
        public ICelestialBody ParentCelestialBody { get; set; }
        public IZome ParentZome { get; set; }
        public HolonEntity ParentHolon { get; set; }
        public IEnumerable<HolonEntity> Children { get; set; }
        public ObservableCollection<HolonEntity> ChildrenTest { get; set; }
        public ObservableCollection<INode> Nodes { get; set; }

        [Column("version_id", TypeName = "NVARCHAR(64)")]
        public Guid VersionId { get; set; }

        [Column("is_new_holon", TypeName = "BOOLEAN")]
        public bool IsNewHolon { get; set; }

        [Column("is_saving", TypeName = "BOOLEAN")]
        public bool IsSaving { get; set; }

        public HolonEntity Original { get; set; }
        public AvatarEntity CreatedByAvatar { get; set; }
        public AvatarEntity ModifiedByAvatar { get; set; }
        public AvatarEntity DeletedByAvatar { get; set; }

        [Column("created_provider_type", TypeName = "INTEGER")]
        public ProviderType CreatedProviderType { get; set; }

        [Column("created_oasis_type", TypeName = "INTEGER")]
        public OASISType CreatedOASISType { get; set; }

        [Column("created_by_avatar_id", TypeName = "NVARCHAR(64)")]
        private Guid CreatedByAvatarId { get; set; }

        [Column("modified_by_avatar_id", TypeName = "NVARCHAR(64)")]
        private Guid ModifiedByAvatarId { get; set; }

        [Column("deleted_by_avatar_id", TypeName = "NVARCHAR(64)")]
        private Guid DeletedByAvatarId { get; set; }
    }
}