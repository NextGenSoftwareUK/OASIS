using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Delivery : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DueDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DispatchedDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DeliveredDate { get; set; }

        public DeliveryStatus Status { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string SentToPhaseId { get; set; }
        public Phase SentToPhase { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string SignedByUserId { get; set; }
        public string SignedByUserFullName { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string DeliveryNoteFileId { get; set; }

        //TODO: TEMP because need to rename correctly in DB and LOTS of records to do! Once renamed in DB, can remove these, because it is now 
        //correct in BaseEntity.
        [BsonRepresentation(BsonType.ObjectId)]
        public string ModifledByUserId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ModifledDate { get; set; }
        public List<DeliveryItem> DeliveryItems { get; set; }
        
    }

    public enum DeliveryStatus
    {
        WaitingToDispatch,
        Dispatched,
        Late,
        Delivered
    }
}