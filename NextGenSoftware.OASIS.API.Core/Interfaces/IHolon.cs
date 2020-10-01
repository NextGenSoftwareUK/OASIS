using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenSoftware.OASIS.API.Core
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
        List<IHolon> Children { get; set; }
        Guid CreatedByAvatarId { get; set; }
        DateTime CreatedDate { get; set; }
        Guid ModifiedByAvatarId { get; set; }
        DateTime ModifiedDate { get; set; }
        Guid DeletedByAvatarId { get; set; }
        DateTime DeletedDate { get; set; }
        bool IsActive { get; set; }
        int Version { get; set; }
    }
}
