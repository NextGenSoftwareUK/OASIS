using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS.Entities
{
    public class HolonBase // Equvilant to the HolonBase object in OASIS.API.Core.
    {
        [BsonId]  
        [BsonRepresentation(BsonType.ObjectId)]  
        public string Id { get; set; }

        public bool IsChanged { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } = new Dictionary<ProviderType, Dictionary<string, string>>(); // Key/Value pair meta data can be stored here, which is unique for that provider.

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>(); // Key/Value pair meta data can be stored here that applies globally across ALL providers.

        public Guid HolonId { get; set; } //Unique id within the OASIS.
        public string Name { get; set; }
        public string Description { get; set; }
        //  public string ProviderKey { get; set; } //Unique key used by each provider (e.g. hashaddress in hc, etc).
        public HolonType HolonType { get; set; }
      //  public ProviderType CreatedProviderType { get; set; }
        public EnumValue<ProviderType> CreatedProviderType { get; set; } // The primary provider that this holon was originally saved with (it can then be auto-replicated to other providers to give maximum redundancy/speed via auto-load balancing etc).
        public EnumValue<OASISType> CreatedOASISType { get; set; }

        //public string ProviderKey { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ProviderType, string> ProviderKey { get; set; } = new Dictionary<ProviderType, string>();

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ModifiedDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DeletedDate { get; set; }

        public int Version { get; set; }
        public Guid PreviousVersionId { get; set; }
        public Dictionary<ProviderType, string> PreviousVersionProviderKey { get; set; } = new Dictionary<ProviderType, string>();
        public bool IsActive { get; set; }

        //  [BsonRepresentation(BsonType.ObjectId)]
        public string CreatedByAvatarId { get; set; }

       // [BsonRepresentation(BsonType.ObjectId)]
        public string ModifiedByAvatarId { get; set; }

       // [BsonRepresentation(BsonType.ObjectId)]
        public string DeletedByAvatarId { get; set; }
    }
}