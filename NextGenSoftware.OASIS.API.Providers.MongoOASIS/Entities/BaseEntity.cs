using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public class BaseEntity
    {
        [BsonId]  
        [BsonRepresentation(BsonType.ObjectId)]  
        public string Id { get; set; }




        public Guid HolonId { get; set; } //Unique id within the OASIS.
        public string Name { get; set; }
        public string Description { get; set; }
        //  public string ProviderKey { get; set; } //Unique key used by each provider (e.g. hashaddress in hc, etc).
        public HolonType HolonType { get; set; }
        public ICelestialBody ParentCelestialBody { get; set; } //The CelestialBody (Star, Planet or Moon) this Holon belongs to.
        public IZome ParentZome { get; set; } //TODO: Wire this up in the HDK.Core.Star code... not used yet because only just added...
        public IHolon Parent { get; set; }
        public IEnumerable<IHolon> Children { get; set; }
        public ProviderType CreatedProviderType { get; set; }
        public List<INode> Nodes { get; set; }


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
        public bool IsActive { get; set; }

        //  [BsonRepresentation(BsonType.ObjectId)]
        public string CreatedByAvatarId { get; set; }

       // [BsonRepresentation(BsonType.ObjectId)]
        public string ModifiedByAvatarId { get; set; }

       // [BsonRepresentation(BsonType.ObjectId)]
        public string DeletedByAvatarId { get; set; }

        /*
        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatedByUserId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ModifledByUserId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string DeletedByUserId { get; set; }
        */
    }
}