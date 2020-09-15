using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class BaseEntity
    {
        [BsonId]  
        [BsonRepresentation(BsonType.ObjectId)]  
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatedByUserId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedDate { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ModifiedByUserId { get; set; }
     //   public string ModifledByUserId { get; set; }
        

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ModifiedDate { get; set; }
        //public DateTime ModifledDate { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string DeletedByUserId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DeletedDate { get; set; }

        public int Version { get; set; }
    }
}