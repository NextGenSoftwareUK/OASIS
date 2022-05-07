//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities
{
    public class HolonBaseEntity // Equvilant to the HolonBase object in OASIS.API.Core. 
    {
        public Guid Id { get; set; }

        public bool IsChanged { get; set; }
        //[NotMapped]
        //public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } = new Dictionary<ProviderType, Dictionary<string, string>>(); // Key/Value pair meta data can be stored here, which is unique for that provider.
        //[NotMapped]
        //public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>(); // Key/Value pair meta data can be stored here that applies globally across ALL providers.

        public Guid HolonId { get; set; } //Unique id within the OASIS.
        public string Name { get; set; }
        public string Description { get; set; }
        //  public string ProviderUniqueStorageKey { get; set; } //Unique key used by each provider (e.g. hashaddress in hc, etc).
        //[NotMapped]
        //public HolonType HolonType { get; set; }
        //  public ProviderType CreatedProviderType { get; set; }
        //[NotMapped] 
        //public EnumValue<ProviderType> CreatedProviderType { get; set; } // The primary provider that this holon was originally saved with (it can then be auto-replicated to other providers to give maximum redundancy/speed via auto-load balancing etc).
        //[NotMapped] 
        //public EnumValue<OASISType> CreatedOASISType { get; set; }

        //public string ProviderUniqueStorageKey { get; set; }
        //[NotMapped] 
        //public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>();

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public DateTime DeletedDate { get; set; }

        public int Version { get; set; }
        public Guid PreviousVersionId { get; set; }

        //[NotMapped] 
        //public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>();

        public bool IsActive { get; set; }

        public string CreatedByAvatarId { get; set; }

        public string ModifiedByAvatarId { get; set; }

        public string DeletedByAvatarId { get; set; }
    }
}
