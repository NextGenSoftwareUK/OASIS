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

        public Material Material { get; set; }

        public int Quantity { get; set; }
    }
}