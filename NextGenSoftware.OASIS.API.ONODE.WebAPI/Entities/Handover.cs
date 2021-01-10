
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Handover : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ContactId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime AcceptedHandoverDate { get; set; }

        public string RejectedReason { get; set; }
    }
}