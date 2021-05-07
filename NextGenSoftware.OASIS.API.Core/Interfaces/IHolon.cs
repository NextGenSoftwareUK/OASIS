using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IHolon
    {
        public IHolon Original { get; set; }
        Guid Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        public bool ChangesSaved { get; set; }
        Dictionary<ProviderType, string> ProviderKey { get; set; }
        Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } 
        Dictionary<string, string> MetaData { get; set; } 
        HolonType HolonType { get; set; }
        public Guid ParentStarId { get; set; } //The Star this Holon belongs to.
        //public ICelestialBody ParentStar { get; set; } //The Star this Holon belongs to.
        public IStar ParentStar { get; set; } //The Star this Holon belongs to.
        public Guid ParentPlanetId { get; set; } //The Planet this Holon belongs to.
        //public ICelestialBody ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public IPlanet ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public Guid ParentMoonId { get; set; } //The Moon this Holon belongs to.
        //public ICelestialBody ParentMoon { get; set; } //The Moon this Holon belongs to.
        public IMoon ParentMoon { get; set; } //The Moon this Holon belongs to.
        //public Guid ParentCelestialBodyId { get; set; } //The CelestialBody (Planet or Moon (OAPP)) this Holon belongs to.
        //public ICelestialBody ParentCelestialBody { get; set; } //The CelestialBody (Planet or Moon (OAPP)) this Holon belongs to.
        public Guid ParentZomeId { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OAPP's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OAPP's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public Guid ParentHolonId { get; set; }
        public IHolon ParentHolon { get; set; }
        IEnumerable<IHolon> Children { get; set; }
        Guid CreatedByAvatarId { get; set; }
        Avatar CreatedByAvatar { get; set; }
        DateTime CreatedDate { get; set; }
        Guid ModifiedByAvatarId { get; set; }
        Avatar ModifiedByAvatar { get; set; }
        DateTime ModifiedDate { get; set; }
        Guid DeletedByAvatarId { get; set; }
        Avatar DeletedByAvatar { get; set; }
        DateTime DeletedDate { get; set; }
        bool IsActive { get; set; }
        int Version { get; set; }
        EnumValue<ProviderType> CreatedProviderType { get; set; }
        List<INode> Nodes { get; set; }
    }
}
