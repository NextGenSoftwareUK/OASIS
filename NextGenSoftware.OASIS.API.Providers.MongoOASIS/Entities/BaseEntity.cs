using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public class BaseEntity
    {
        [BsonId]  
        [BsonRepresentation(BsonType.ObjectId)]  
        public string Id { get; set; }

        public string ProviderKey { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ModifiedDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DeletedDate { get; set; }

        public int Version { get; set; }

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