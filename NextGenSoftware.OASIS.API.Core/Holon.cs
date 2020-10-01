using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core
{
    public class Holon : IHolon
    {
        public Guid Id { get; set; } //Unique id within the OASIS.
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProviderKey { get; set; } //Unique key used by each provider (e.g. hashaddress in hc, etc).
        public HolonType HolonType { get; set; }
        public ICelestialBody CelestialBody { get; set; } //The CelestialBody (Star, Planet or Moon) this Holon belongs to.
        public IZome ParentZome { get; set; } //TODO: Wire this up in the HDK.Core.Star code... not used yet because only just added...
        public IHolon Parent { get; set; }
        public List<IHolon> Children { get; set; }
        public Guid CreatedByAvatarId { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid ModifiedByAvatarId { get; set; }
        public DateTime ModifiedDate { get; set; }
        public Guid DeletedByAvatarId { get; set; }
        public DateTime DeletedDate { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
    }
}
