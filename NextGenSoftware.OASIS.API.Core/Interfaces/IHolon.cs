using System;
using System.Collections.Generic;

using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IHolon
    {
        Guid Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        string ProviderKey { get; set; }
        HolonType HolonType { get; set; }
        ICelestialBody CelestialBody { get; set; }
        IZome ParentZome { get; set; }
        IHolon Parent { get; set; }
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
        EnumValue<ProviderType> ProviderType { get; set; }
        public List<INode> Nodes { get; set; }
    }
}
