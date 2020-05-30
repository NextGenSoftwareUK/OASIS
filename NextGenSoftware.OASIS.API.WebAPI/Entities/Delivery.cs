using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class Delivery
    {
        [BsonId]  
        [BsonRepresentation(BsonType.ObjectId)]  
        public string Id { get; set; }
        //public int Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DueDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime SentDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DeliveredDate { get; set; }

        public int Status { get; set; }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string SentToPhaseId { get; set; }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string SignedByUserId { get; set; }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string FileId { get; set; }
    }
}