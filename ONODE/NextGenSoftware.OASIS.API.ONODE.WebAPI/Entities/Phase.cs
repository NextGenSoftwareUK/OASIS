using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Phase : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string SequenceId { get; set; }

        public Sequence Sequence { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime StartDate { get; set; }
        
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime EndDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ActualStartDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ActualEndDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime PhaseHandedOverDate { get; set; }


        public int PhaseNo { get; set; }
        public string PhaseTitle { get; set; }
        public string ScopeDescription { get; set; }

        //  [BsonRepresentation(BsonType.ObjectId)]
        // public string BIMModelFileId { get; set; }


        //TODO: TEMP because need to rename correctly in DB and LOTS of records to do! Once renamed in DB, can remove these, because it is now 
        //correct in BaseEntity.
        [BsonRepresentation(BsonType.ObjectId)]
        public string ModifledByUserId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ModifledDate { get; set; }
    }
}