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
        public Guid HolonId { get; set; }

        public Guid ParentOmniverseId { get; set; }

        public Guid ParentMultiverseId { get; set; }

        public Guid ParentUniverseId { get; set; }

        public Guid ParentDimensionId { get; set; }

        public Guid ParentGalaxyClusterId { get; set; }

        public Guid ParentGalaxyId { get; set; }

        public Guid ParentSolarSystemId { get; set; }

        public Guid ParentGreatGrandSuperStarId { get; set; }

        public Guid ParentGrandSuperStarId { get; set; }

        public Guid ParentSuperStarId { get; set; }

        public Guid ParentStarId { get; set; }

        public Guid ParentPlanetId { get; set; }

        public Guid ParentMoonId { get; set; }

        public Guid ParentCelestialSpaceId { get; set; }

        public Guid ParentCelestialBodyId { get; set; }

        public Guid ParentZomeId { get; set; }

        public Guid ParentHolonId { get; set; }

        public IOmiverse ParentOmniverse { get; set; }

        public IMultiverse ParentMultiverse { get; set; }

        public IUniverse ParentUniverse { get; set; }

        public IDimension ParentDimension { get; set; }

        public DimensionLevel DimensionLevel { get; set; }

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

        public Guid VersionId { get; set; }

        public bool IsNewHolon { get; set; }

        public bool IsSaving { get; set; }
        public HolonEntity Original { get; set; }
        public AvatarEntity CreatedByAvatar { get; set; }
        public AvatarEntity ModifiedByAvatar { get; set; }
        public AvatarEntity DeletedByAvatar { get; set; }

        public ProviderType CreatedProviderType { get; set; }

        public OASISType CreatedOASISType { get; set; }

        private Guid CreatedByAvatarId { get; set; }

        private Guid ModifiedByAvatarId { get; set; }

        private Guid DeletedByAvatarId { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime DeletedDate { get; set; }

        public string Description { get; set; }

        public Guid Id { get; set; }

        public bool IsActive { get; set; }

        public bool IsChanged { get; set; }

        public DateTime ModifiedDate { get; set; }

        public string Name { get; set; }

        public Guid PreviousVersionId { get; set; }

        public int Version { get; set; }
    }
}