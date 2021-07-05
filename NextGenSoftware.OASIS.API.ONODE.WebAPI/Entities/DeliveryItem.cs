using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class DeliveryItem : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string DeliveryId { get; set; }

        //[BsonRepresentation(BsonType.ObjectId)]
        //public string MaterialId { get; set; }

        //public Material Material { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string FileId { get; set; }

        public File File { get; set; }

        public int Quantity { get; set; }

        //TODO: TEMP because need to rename correctly in DB and LOTS of records to do! Once renamed in DB, can remove these, because it is now 
        //correct in BaseEntity.
        [BsonRepresentation(BsonType.ObjectId)]
        public string ModifledByUserId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ModifledDate { get; set; }
    }
}