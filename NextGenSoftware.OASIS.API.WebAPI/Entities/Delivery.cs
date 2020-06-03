using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class Delivery : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DueDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime SentDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DeliveredDate { get; set; }

        public int Status { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string SentToPhaseId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string SignedByUserId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string DeliveryNoteFileId { get; set; }

        public List<DeliveryItem> DeliveryItems { get; set; }
        
    }
}