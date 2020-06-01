using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class DeliveryItem : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string DeliveryId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string MaterialId { get; set; }
        public int Quanitiy { get; set; }
    }
}