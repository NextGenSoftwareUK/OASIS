using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class Holon : IHolon
    {
        public Guid Id { get; set; } //Unique id within the OASIS.
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<ProviderType, string> ProviderKey { get; set; } = new Dictionary<ProviderType, string>(); //Unique key used by each provider (e.g. hashaddress in hc, accountname for Telos, id in MongoDB etc).        
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } = new Dictionary<ProviderType, Dictionary<string, string>>(); // Key/Value pair meta data can be stored here, which is unique for that provider.
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>(); // Key/Value pair meta data can be stored here that applies globally across ALL providers.
        public HolonType HolonType { get; set; }
        public ICelestialBody ParentCelestialBody { get; set; } //The CelestialBody (Star, Planet or Moon) this Holon belongs to.
        public IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OAPP's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public IHolon Parent { get; set; }
        public Guid ParentId { get; set; }
        public IEnumerable<IHolon> Children { get; set; }
        public Guid CreatedByAvatarId { get; set; }
        public Avatar CreatedByAvatar { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid ModifiedByAvatarId { get; set; }
        public Avatar ModifiedByAvatar { get; set; }
        public DateTime ModifiedDate { get; set; }
        public Guid DeletedByAvatarId { get; set; }
        public Avatar DeletedByAvatar { get; set; }
        public DateTime DeletedDate { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public EnumValue<ProviderType> CreatedProviderType { get; set; } // The primary provider that this holon was originally saved with (it can then be auto-replicated to other providers to give maximum redundancy/speed via auto-load balancing etc).
        public List<INode> Nodes { get; set; } // List of nodes/fields (int, string, bool, etc) that belong to this Holon (STAR ODK auto-generates these when generating dynamic code from DNA Templates passed in).
    }
}
